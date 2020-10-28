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