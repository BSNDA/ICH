pragma solidity ^0.4.25;

import "./IEthCrossChainManager.sol";
import "./IEthCrossChainManagerProxy.sol";

/**
 * @author kuan
 * @title Poly cross-chain contract
 * @dev Calling process (steps 1 and 2 are one-time calling. After binding, you can repeatedly call say method):
 *      1. call setManagerProxy to bind the management contract
 *      2. call bindProxyHash to bind the target framework's chainId and application contract
 *      3. call say method for cross-chain operation
 */
contract HelloPoly {

    //Store the address of the source chain's management contract
    address public managerProxyContract;
    //Store the address of target chain's cross-chain contracts
    mapping(uint64 => bytes) public proxyHashMap;
    //Responsible for receiving the results of other cross-chain contracts calling say method
    bytes  public hearSomeThing;

    event SetManagerProxyEvent(address manager);
    event BindProxyEvent(uint64 toChainId, bytes targetProxyHash);
    event Say(uint64 toChainId, bytes toContractAddress, bytes somethingWoW);
    event Hear(bytes somethingWoW, bytes fromContractAddr);


    /**
     * @dev Set up the management contract
     * @param _managerProxyContract The address of the source chain's management contract
     **/
    function setManagerProxy(address _managerProxyContract) public {
        managerProxyContract = _managerProxyContract;
        emit SetManagerProxyEvent(managerProxyContract);
    }


    /**
     * @dev Bind the application contract to be called
     * @param _toChainId The target chain's chainID
     * @param _targetProxyHash The target chain's application address
     * @return bool
     **/
    function bindProxyHash(uint64 _toChainId, bytes memory _targetProxyHash) public returns (bool) {
        proxyHashMap[_toChainId] = _targetProxyHash;
        emit BindProxyEvent(_toChainId, _targetProxyHash);
        return true;
    }

    /**
     * @dev Enable the cross-chain invocation by calling the say method
     * @param _toChainId The target chain's chainID
     * @param _functionName The target chain's function name in the contract
     * @param _somethingWoW Parameters passed across the chain
     * @return bool
     **/
    function say(uint64 _toChainId, string memory _functionName, string memory _somethingWoW) public returns (bool){
        //Get the cross-chain management contract interface
        IEthCrossChainManagerProxy eccmp = IEthCrossChainManagerProxy(managerProxyContract);
        //Get the address of the cross-chain management contract
        address eccmAddr = eccmp.getEthCrossChainManager();
        //Get the cross-chain manager contract object
        IEthCrossChainManager eccm = IEthCrossChainManager(eccmAddr);
        //Get the address of the target chain application contract
        bytes memory toProxyHash = proxyHashMap[_toChainId];
        //Call the cross-chain method
        require(eccm.crossChain(_toChainId, toProxyHash, bytes(_functionName), bytes(_somethingWoW)),"EthCrossChainManager crossChain executed error!");
        emit Say(_toChainId,toProxyHash, bytes(_somethingWoW));
        return true;
    }

    /**
     * @param _somethingWoW Parameters passed across the chain
     * @param _fromContractAddr The target chain's application address
     * @param _toChainId The target chain's chainID
     * @return bool
     **/
    function hear(bytes memory _somethingWoW, bytes memory _fromContractAddr, uint64 _toChainId) public returns (bool){
        hearSomeThing = _somethingWoW;
        emit Hear(_somethingWoW, _fromContractAddr);
        return true;
    }

}