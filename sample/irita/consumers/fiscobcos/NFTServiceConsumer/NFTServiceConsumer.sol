pragma solidity ^0.4.25;

/**
 * @title iService interface
 */
interface iServiceInterface {
    /**
     * @notice Initiate a service invocation
     * @param _serviceName Service name
     * @param _input Request input
     * @param _timeout Request timeout
     * @param _callbackAddress Callback contract address
     * @param _callbackFunction Callback function selector
     * @return requestID Request id
     */
    function callService(
        string _serviceName,
        string _input,
        uint256 _timeout,
        address _callbackAddress,
        bytes4 _callbackFunction
    ) external returns (bytes32 requestID);
}

/*
 * @title Contract for the iService Consumer Proxy client
 */
contract iServiceClient {
    iServiceInterface iServiceConsumerProxy; // iService Consumer Proxy contract address

    // mapping the request id to RequestStatus
    mapping(bytes32 => RequestStatus) requests;

    // request status
    struct RequestStatus {
        bool sent; // request sent
        bool responded; // request responded
    }

    /*
     * @notice Event triggered when the iService request is sent
     * @param _requestID Request id
     */
    event IServiceRequestSent(bytes32 _requestID);

    /*
     * @notice Make sure that the given request is valid
     * @param _requestID Request id
     */
    modifier validRequest(bytes32 _requestID) {
        require(requests[_requestID].sent, "iServiceClient: request does not exist");
        require(!requests[_requestID].responded, "iServiceClient: request has been responded");

        _;
    }

    /*
     * @notice Send iService request
     * @param _serviceName Service name
     * @param _input Service request input
     * @param _timeout Service request timeout
     * @param _callbackAddress Callback contract address
     * @param _callbackFunction Callback function selector
     * @return Request id
     */
    function sendIServiceRequest(
        string memory _serviceName,
        string memory _input,
        uint256 _timeout,
        address _callbackAddress,
        bytes4 _callbackFunction
    )
    internal
    returns (bytes32 requestID)
    {
        requestID = iServiceConsumerProxy.callService(_serviceName, _input, _timeout, _callbackAddress, _callbackFunction);

        emit IServiceRequestSent(requestID);

        requests[requestID].sent = true;

        return requestID;
    }

    /**
     * @notice Set the iService Consumer Proxy contract address
     * @param _iServiceConsumerProxy The address of the iService Consumer Proxy contract
     */
    function setIServiceConsumerProxy(address _iServiceConsumerProxy) internal {
        iServiceConsumerProxy = iServiceInterface(_iServiceConsumerProxy);
    }
}

/*
 * @title Contract for interchain NFT minting powered by iService
 */
contract NFTServiceConsumer is iServiceClient {
    // nft service name
    string private nftServiceName = "nft"; // nft service name

    // nft request header
    string private header = '{"header":{},'; // request header fot nft service

    // minting result
    string public mintResult; // result of the nft minting

    uint256 private defaultTimeout = 100; // maximum number of irita-hub blocks to wait for; default to 100

    /*
     * @notice Event triggered when the nft is minted
     * @param _requestID Request id
     * @param _mintResult NFT minting result
     */
    event NFTMinted(bytes32 _requestID, string _mintResult);

    /*
     * @notice Constructor
     * @param _iServiceCore Address of the iService Core Extension contract
     */
    constructor(
        address _iServiceConsumerProxy
    )
    public
    {
        setIServiceConsumerProxy(_iServiceConsumerProxy);
    }

    /*
     * @notice Start to mint nft
     * This method is not gas efficient due to string operation
     * @param _to Destination address to mint to
     * @param _amount Amount of NFTs to be minted
     * @param _metaID Metadata id
     */
    function mint (
        string _to,
        string _amount,
        string _metaID
    )
    external
    {
        string memory request = _buildMintRequest(_to, _amount, _metaID);

        sendIServiceRequest(
            nftServiceName,
            request,
            defaultTimeout,
            address(this),
            this.onNFTMinted.selector
        );
    }

    /*
     * @notice Start to mint nft
     * This method needs less gas than mint()
     * @param _args Arguments for minting NFT
     * For example:
     * {"header":{},"body":{"to":"<address>",
     * "amount_to_mint":"1","meta_id":"test-id","set_price":"0",
     * "is_for_sale":false}}
     */
    function mintV2 (
        string _args
    )
    external
    {
        sendIServiceRequest(
            nftServiceName,
            _args,
            defaultTimeout,
            address(this),
            this.onNFTMinted.selector
        );
    }

    /*
     * @notice NFT service callback function
     * @param _requestID Request id
     * @param _output NFT service response output
     */
    function onNFTMinted(
        bytes32 _requestID,
        string _output
    )
    external
    {
        mintResult = _output;
        emit NFTMinted(_requestID, mintResult);
    }

    /*
     * @notice Build the nft minting request
     * @param _to Destination address to mint to
     * @param _amount Amount of NFTs to be minted
     * @param _metaID Metadata id
     */
    function _buildMintRequest(
        string _to,
        string _amount,
        string memory _metaID
    )
    internal
    view
    returns (string memory)
    {
        string memory body = _strConcat(_strConcat('"body":{"to":"', _to),'"');
        body = _strConcat(_strConcat(body,',"amount_to_mint":"'), _amount);
        body = _strConcat(_strConcat(body,'","meta_id":"'), _metaID);
        body = _strConcat(body,'","set_price":"0"}}');

        return _strConcat(header, body);
    }

    /*
     * @notice Concatenate two strings into a single string
     * @param _first First string
     * @param _second Second string
     */
    function _strConcat(
        string memory _first,
        string memory _second
    )
    internal
    pure
    returns(string memory)
    {
        bytes memory first = bytes(_first);
        bytes memory second = bytes(_second);
        bytes memory res = new bytes(first.length + second.length);

        for(uint i = 0; i < first.length; i++) {
            res[i] = first[i];
        }

        for(uint j = 0; j < second.length; j++) {
            res[first.length+j] = second[j];
        }

        return string(res);
    }
}