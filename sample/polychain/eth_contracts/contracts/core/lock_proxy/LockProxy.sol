pragma solidity ^0.5.0;

import "./../../libs/ownership/Ownable.sol";
import "./../../libs/common/ZeroCopySource.sol";
import "./../../libs/common/ZeroCopySink.sol";
import "./../../libs/utils/Utils.sol";
import "./../../libs/token/ERC20/SafeERC20.sol";
import "./../cross_chain_manager/interface/IEthCrossChainManager.sol";
import "./../cross_chain_manager/interface/IEthCrossChainManagerProxy.sol";


contract LockProxy is Ownable {
    using SafeMath for uint;
    using SafeERC20 for IERC20;

    struct TxArgs {
        bytes toAssetHash;
        bytes toAddress;
        uint256 amount;
    }

    address public managerProxyContract;
    mapping(uint64 => bytes) public proxyHashMap;
    mapping(address => mapping(uint64 => bytes)) public assetHashMap;
    mapping(address => bool) safeTransfer;

    event SetManagerProxyEvent(address manager);
    event BindProxyEvent(uint64 toChainId, bytes targetProxyHash);
    event BindAssetEvent(address fromAssetHash, uint64 toChainId, bytes targetProxyHash, uint initialAmount);
    event UnlockEvent(address toAssetHash, address toAddress, uint256 amount);
    event LockEvent(address fromAssetHash, address fromAddress, uint64 toChainId, bytes toAssetHash, bytes toAddress, uint256 amount);

    modifier onlyManagerContract() {
        IEthCrossChainManagerProxy ieccmp = IEthCrossChainManagerProxy(managerProxyContract);
        require(_msgSender() == ieccmp.getEthCrossChainManager(), "msgSender is not EthCrossChainManagerContract");
        _;
    }

    function setManagerProxy(address ethCCMProxyAddr) onlyOwner public {
        managerProxyContract = ethCCMProxyAddr;
        emit SetManagerProxyEvent(managerProxyContract);
    }

    function bindProxyHash(uint64 toChainId, bytes memory targetProxyHash) onlyOwner public returns (bool) {
        proxyHashMap[toChainId] = targetProxyHash;
        emit BindProxyEvent(toChainId, targetProxyHash);
        return true;
    }

    function bindAssetHash(address fromAssetHash, uint64 toChainId, bytes memory toAssetHash) onlyOwner public returns (bool) {
        assetHashMap[fromAssetHash][toChainId] = toAssetHash;
        emit BindAssetEvent(fromAssetHash, toChainId, toAssetHash, getBalanceFor(fromAssetHash));
        return true;
    }

    /* @notice                   此方法用于ETH对其它目标链进行跨链调用
     *  @param fromAssetHash     在以太坊上想要跨链的资产合约地址，比如ETH为0x0000000000000000000000000000000000000000；
     *  @param toChainId         跨链的目的地，即目标链的chainID，这个ID是注册在Poly合约中的，是跨链协议的一部分，比如以太坊是2，本体是3
     *  @param toAddress         目标链接收ETH的地址哈希值，这里要填写原哈希值，而不是经过变形的地址，比如本体base58的地址 (AdzZ2VKufdJWeB8t9a8biXoHbbMe2kZeyH，它的原哈希值为0xf3b8a17f1f957f60c88f105e32ebff3f022e56a4)
     *  @param amount            要跨链的ETH金额，以wei为单位;
     */
    function lock(address fromAssetHash, uint64 toChainId, bytes memory toAddress, uint256 amount) public payable returns (bool) {
        // 判断跨链的ETH金额是否为0
        require(amount != 0, "amount cannot be zero!");

        // 将用户的ETH锁定到LockProxy合约地址里
        require(_transferToContract(fromAssetHash, amount), "transfer asset from fromAddress to lock_proxy contract  failed!");

        // 取出提前绑定好目标链的资产地址(这个地址代表目标链的资产合约,该合约会接收跨链的ETH，相当于ETH在目标链的映射资产，跨链后该合约的账本中toAddress会增加amount个ETH)
        bytes memory toAssetHash = assetHashMap[fromAssetHash][toChainId];

        // 判断资产地址是否为空
        require(toAssetHash.length != 0, "empty illegal toAssetHash");

        // 构建跨链请求信息
        TxArgs memory txArgs = TxArgs({
            toAssetHash : toAssetHash,
            toAddress : toAddress,
            amount : amount
            });

        // 将跨链请求信息进行序列化
        bytes memory txData = _serializeTxArgs(txArgs);

        // 构建跨链管理代理
        IEthCrossChainManagerProxy eccmp = IEthCrossChainManagerProxy(managerProxyContract);

        // 获取跨链管理合约地址
        address eccmAddr = eccmp.getEthCrossChainManager();

        // 根据跨链管理合约地址获取跨链管理接口
        IEthCrossChainManager eccm = IEthCrossChainManager(eccmAddr);

        // 从应用合约中获取提前绑定好的toProxyHash，这是目标链上的应用合约
        bytes memory toProxyHash = proxyHashMap[toChainId];

        // 检查目标链应用合约地址长度是否为空
        require(toProxyHash.length != 0, "empty illegal toProxyHash");

        // 把跨链信息txData发送到toChainId代表的目标链（注：unlock方法为目标链应用合约实现inbound功能所对应的方法，此方法由目标链自行定义）
        require(eccm.crossChain(toChainId, toProxyHash, "unlock", txData), "EthCrossChainManager crossChain executed error!");

        // 将消息以事件的方式通知给Relayer
        emit LockEvent(fromAssetHash, _msgSender(), toChainId, toAssetHash, toAddress, amount);

        return true;

    }

    /* @notice                  此方法用于其它目标链进行跨链调用(此方法可以自行定义)
    *  @param argsBs            跨链的原始信息
    *  @param fromContractAddr  源链的应用合约地址
    *  @param fromChainId       源链的chainID
    */
    function unlock(bytes memory argsBs, bytes memory fromContractAddr, uint64 fromChainId) onlyManagerContract public returns (bool) {
        // 反序列化跨链信息获得args
        TxArgs memory args = _deserializeTxArgs(argsBs);
        
        // 检查源链的应用合约地址是否为空值
        require(fromContractAddr.length != 0, "from proxy contract address cannot be empty");
        
        // 检查源链的链ID以及源链的应用合约信息与本地存储是否相等
        require(Utils.equalStorage(proxyHashMap[fromChainId], fromContractAddr), "From Proxy contract address error!");
        
        // 检查目标链资产合约是否为空值
        require(args.toAssetHash.length != 0, "toAssetHash cannot be empty");
        
        // 获取目标链资产合约
        address toAssetHash = Utils.bytesToAddress(args.toAssetHash);
        
        // 检查目标链地址是否为空值
        require(args.toAddress.length != 0, "toAddress cannot be empty");
        
        // 获取目标链地址
        address toAddress = Utils.bytesToAddress(args.toAddress);
        
        // 解锁对应数量amount的toAssetHash资产给toAddress
        require(_transferFromContract(toAssetHash, toAddress, args.amount), "transfer asset from lock_proxy contract to toAddress failed!");
        
        // 将消息以事件的方式通知给Relayer
        emit UnlockEvent(toAssetHash, toAddress, args.amount);
        
        return true;
    }

    function getBalanceFor(address fromAssetHash) public view returns (uint256) {
        if (fromAssetHash == address(0)) {
            // return address(this).balance; // this expression would result in error: Failed to decode output: Error: insufficient data for uint256 type
            address selfAddr = address(this);
            return selfAddr.balance;
        } else {
            IERC20 erc20Token = IERC20(fromAssetHash);
            return erc20Token.balanceOf(address(this));
        }
    }

    function _transferToContract(address fromAssetHash, uint256 amount) internal returns (bool) {
        if (fromAssetHash == address(0)) {
            // fromAssetHash === address(0) denotes user choose to lock ether
            // passively check if the received msg.value equals amount
            require(msg.value != 0, "transferred ether cannot be zero!");
            require(msg.value == amount, "transferred ether is not equal to amount!");
        } else {
            // make sure lockproxy contract will decline any received ether
            require(msg.value == 0, "there should be no ether transfer!");
            // actively transfer amount of asset from msg.sender to lock_proxy contract
            require(_transferERC20ToContract(fromAssetHash, _msgSender(), address(this), amount), "transfer erc20 asset to lock_proxy contract failed!");
        }
        return true;
    }

    function _transferFromContract(address toAssetHash, address toAddress, uint256 amount) internal returns (bool) {
        if (toAssetHash == address(0x0000000000000000000000000000000000000000)) {
            // toAssetHash === address(0) denotes contract needs to unlock ether to toAddress
            // convert toAddress from 'address' type to 'address payable' type, then actively transfer ether
            address(uint160(toAddress)).transfer(amount);
        } else {
            // actively transfer amount of asset from msg.sender to lock_proxy contract
            require(_transferERC20FromContract(toAssetHash, toAddress, amount), "transfer erc20 asset to lock_proxy contract failed!");
        }
        return true;
    }


    function _transferERC20ToContract(address fromAssetHash, address fromAddress, address toAddress, uint256 amount) internal returns (bool) {
        IERC20 erc20Token = IERC20(fromAssetHash);
        //  require(erc20Token.transferFrom(fromAddress, toAddress, amount), "trasnfer ERC20 Token failed!");
        erc20Token.safeTransferFrom(fromAddress, toAddress, amount);
        return true;
    }

    function _transferERC20FromContract(address toAssetHash, address toAddress, uint256 amount) internal returns (bool) {
        IERC20 erc20Token = IERC20(toAssetHash);
        //  require(erc20Token.transfer(toAddress, amount), "trasnfer ERC20 Token failed!");
        erc20Token.safeTransfer(toAddress, amount);
        return true;
    }

    function _serializeTxArgs(TxArgs memory args) internal pure returns (bytes memory) {
        bytes memory buff;
        buff = abi.encodePacked(
            ZeroCopySink.WriteVarBytes(args.toAssetHash),
            ZeroCopySink.WriteVarBytes(args.toAddress),
            ZeroCopySink.WriteUint255(args.amount)
        );
        return buff;
    }

    function _deserializeTxArgs(bytes memory valueBs) internal pure returns (TxArgs memory) {
        TxArgs memory args;
        uint256 off = 0;
        (args.toAssetHash, off) = ZeroCopySource.NextVarBytes(valueBs, off);
        (args.toAddress, off) = ZeroCopySource.NextVarBytes(valueBs, off);
        (args.amount, off) = ZeroCopySource.NextUint255(valueBs, off);
        return args;
    }
}