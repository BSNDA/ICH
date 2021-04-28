pragma solidity ^0.4.4;

import "./IEthCrossChainManager.sol";
import "./IEthCrossChainManagerProxy.sol";

/**
 * @author kuan
 * @title Poly跨链合约
 * @dev 调用流程（1、2步骤都是一次性，绑定之后可以多次调用say）：
 *      1. 调用setManagerProxy绑定管理合约
 *      2. 调用bindProxyHash绑定对方框架的chainId和应用合约
 *      3. 调用say进行跨链操作
 */
contract HelloPoly {

    //存储本框架管理合约的地址
    address public managerProxyContract;
    //存储需要调用的其他跨链合约的合约地址
    mapping(uint64 => bytes) public proxyHashMap;
    //负责接收其他跨链合约say的结果
    bytes public hearSomeThing;
    //负责记录跨链合约say的参数
    bytes public saySomeThing;

    event SetManagerProxyEvent(address manager);
    event BindProxyEvent(uint64 toChainId, bytes targetProxyHash);
    event Say(uint64 toChainId, bytes toContractAddress, bytes somethingWoW);
    event Hear(bytes somethingWoW, bytes fromContractAddr);


    /**
     * @dev 设置管理合约
     * @param _managerProxyContract 本框架管理合约的地址
     * @return 
     **/
    function setManagerProxy(address _managerProxyContract) public {
        managerProxyContract = _managerProxyContract;
        emit SetManagerProxyEvent(managerProxyContract);
    }


    /**
     * @dev 绑定需要调用的应用合约
     * @param _toChainId 被调用的合约框架chainId
     * @param _targetProxyHash 被调用的应用合约地址
     * @return bool
     **/
    function bindProxyHash(uint64 _toChainId, bytes memory _targetProxyHash) public returns (bool) {
        proxyHashMap[_toChainId] = _targetProxyHash;
        emit BindProxyEvent(_toChainId, _targetProxyHash);
        return true;
    }

    /**
     * @dev 通过调用say方法实现跨链调用
     * @param _toChainId 被调用的合约框架chainId
     * @param _somethingWoW 跨链传递的参数
     * @return bool
     **/
    function say(uint64 _toChainId, bytes _somethingWoW) public returns (bool){
        //获取跨链管理合约接口
        IEthCrossChainManagerProxy eccmp = IEthCrossChainManagerProxy(managerProxyContract);
        //获取跨链管理合约地址
        address eccmAddr = eccmp.getEthCrossChainManager();
        //获取跨链管理合约对象
        IEthCrossChainManager eccm = IEthCrossChainManager(eccmAddr);
        //获取目标链应用合约地址
        bytes memory toProxyHash = proxyHashMap[_toChainId];
        //调用跨链
        require(eccm.crossChain(_toChainId, toProxyHash, "hear", _somethingWoW), "CrossChainManager crossChain executed error!");
        saySomeThing = _somethingWoW;
        emit Say(_toChainId, toProxyHash, _somethingWoW);
        return true;
    }

    /**
     * @param _somethingWoW 跨链传递的参数
     * @param _fromContractAddr 被调用的应用合约地址
     * @param _toChainId 被调用的合约框架chainId
     * @return bool
     **/
    function hear(bytes _somethingWoW, bytes _fromContractAddr, uint64 _toChainId) public returns (bool){
        hearSomeThing = _somethingWoW;
        emit Hear(_somethingWoW, _fromContractAddr);
        return true;
    }

}
