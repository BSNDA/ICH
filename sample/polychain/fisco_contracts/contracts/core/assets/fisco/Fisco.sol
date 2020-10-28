pragma solidity ^0.5.0;

import "./../../../libs/token/ERC20/ERC20Detailed.sol";
import "./../../../libs/GSN/Context.sol";
import "./../../../libs/token/ERC20/ERC20.sol";

contract FISCOX is Context, ERC20, ERC20Detailed {
    
    constructor (address lockProxyContractAddress) public ERC20Detailed("pETH", "pETH", 18) {
        _mint(lockProxyContractAddress, 1000000000000000000);
    }
}