pragma solidity ^0.4.4;

contract Store {
    // data store
    mapping (bytes32 => string) public store;

    // count
    uint256 private count;

    // event triggered when some value is stored
    event Set(bytes32 key, string value);

    // store data
    function set(string value) external {
        bytes32 key = getKey();
        store[key] = value;

        emit Set(key, value);
    }

    // get the store key
    function getKey() internal returns(bytes32) {
        bytes32 key = keccak256(abi.encodePacked(this, count));
        count += 1;

        return key;
    }
}