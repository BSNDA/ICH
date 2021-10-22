package chaincode

import (
	"encoding/json"
	"fmt"
	"github.com/BSNDA/ICH/sample/irita/consumers/fabric/crosschaincode"
	"github.com/hyperledger/fabric/core/chaincode/shim"
	"github.com/hyperledger/fabric/protos/peer"
	"strings"
)

//["跨链合约名称","目标链Id","目标链请求类型","目标链合约名","目标链参数","回调合约名","回调合约方法"]
//["irita_app00001","100001","query","getkey","[\"a\"]"，"callbackCC","callbackfunc"]
func (c *SCChaincode) callFabric(stub shim.ChaincodeStubInterface, args []string) peer.Response {

	req := &CrossFabricReqest{}
	err := json.Unmarshal([]byte(args[0]), req)
	if err != nil {
		return shim.Error(fmt.Sprintf("args error；%s", err))
	}

	req.TargetType = getFabricEndpointType(req.TargetType)

	reqId, err := crosschaincode.CallCrossService(stub,
		req.CrossChaincodeName,
		req.TargetChainId,
		req.TargetChaincodeName,
		req.TargetType,
		req.TargetArgs,
		req.CallBackChaincodeName,
		req.CallBackChaincodeFunctionName)
	if err != nil {
		return shim.Error("callFabric has failed ," + err.Error())
	}
	fmt.Println(reqId)

	cd := &CrossData{
		Id:    reqId,
		Input: args[0],
	}

	cdb, _ := json.Marshal(cd)
	if err := stub.PutState(crossKey(reqId), cdb); err != nil {
		return shim.Error(fmt.Sprintf("put data info error；%s", err))
	}

	return shim.Success([]byte(reqId))
}

func (c *SCChaincode) callFisco(stub shim.ChaincodeStubInterface, args []string) peer.Response {

	req := &CrossFabricReqest{}

	err := json.Unmarshal([]byte(args[0]), req)
	if err != nil {
		return shim.Error(fmt.Sprintf("args error；%s", err))
	}

	req.TargetType = getFiscoEndpointType(req.TargetType)

	reqId, err := crosschaincode.CallCrossService(stub,
		req.CrossChaincodeName,
		req.TargetChainId,
		req.TargetChaincodeName,
		req.TargetType,
		req.TargetArgs,
		req.CallBackChaincodeName,
		req.CallBackChaincodeFunctionName)
	if err != nil {
		return shim.Error("callFisco has failed ," + err.Error())
	}
	fmt.Println(reqId)

	cd := &CrossData{
		Id:    reqId,
		Input: args[0],
	}

	cdb, _ := json.Marshal(cd)
	if err := stub.PutState(crossKey(reqId), cdb); err != nil {
		return shim.Error(fmt.Sprintf("put data info error；%s", err))
	}

	return shim.Success([]byte(reqId))
}

func (c *SCChaincode) callNewChainLink(stub shim.ChaincodeStubInterface, args []string) peer.Response {

	req := &CrossFabricReqest{}
	err := json.Unmarshal([]byte(args[0]), req)
	if err != nil {
		return shim.Error(fmt.Sprintf("args error；%s", err))
	}

	req.TargetType = crosschaincode.EndpointType_Service
	reqId, err := crosschaincode.CallCrossService(stub,
		req.CrossChaincodeName,
		req.TargetChainId,
		req.TargetChaincodeName,
		req.TargetType,
		req.TargetArgs,
		req.CallBackChaincodeName,
		req.CallBackChaincodeFunctionName)

	if err != nil {
		return shim.Error("callFisco has failed ," + err.Error())
	}
	fmt.Println(reqId)

	cd := &CrossData{
		Id:    reqId,
		Input: args[0],
	}

	cdb, _ := json.Marshal(cd)
	if err := stub.PutState(crossKey(reqId), cdb); err != nil {
		return shim.Error(fmt.Sprintf("put data info error；%s", err))
	}

	return shim.Success([]byte(reqId))
}

func getFabricEndpointType(t string) string {

	if strings.ToLower(t) == "invoke" {
		return crosschaincode.EndpointType_Tx
	} else {
		return crosschaincode.EndpointType_Call
	}
}

func getFiscoEndpointType(t string) string {

	if strings.ToLower(t) == "tx" {
		return crosschaincode.EndpointType_Tx
	} else {
		return crosschaincode.EndpointType_Call
	}
}
