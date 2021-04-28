package chaincode

import (
	"encoding/json"
	"fmt"
	"github.com/BSNDA/ICH/sample/irita/consumers/fabric/crosschaincode"
	"github.com/hyperledger/fabric/core/chaincode/shim"
	"github.com/hyperledger/fabric/protos/peer"
	"strconv"
)

func (c *SCChaincode) callFabric(stub shim.ChaincodeStubInterface, args []string) peer.Response {

	if len(args) < 7 {
		return shim.Error("Length cannot be less than 7")
	}

	serviceName := args[0]
	crossChaincode := args[1]
	cb_cc := args[2]
	chainIdStr := args[3]

	chainId, err := strconv.ParseUint(chainIdStr, 10, 64)
	if err != nil {
		return shim.Error("Incorrect chainID type")
	}

	chainCode := args[4]
	funType := args[5]
	args = args[6:]

	input := &crosschaincode.FabricInput{
		ChainId:   chainId,
		ChainCode: chainCode,
		FunType:   funType,
		Args:      args,
	}

	cb_fcn := "callback"

	reqId, err := crosschaincode.CallCrossService(stub, serviceName, crossChaincode, input, cb_cc, cb_fcn, 100)
	if err != nil {
		return shim.Error("callFabric has failed ," + err.Error())
	}
	fmt.Println(reqId)

	jsonBytes, _ := json.Marshal(input)
	cd := &CrossData{
		Id:    reqId,
		Input: string(jsonBytes),
	}

	cdb, _ := json.Marshal(cd)
	if err := stub.PutState(crossKey(reqId), cdb); err != nil {
		return shim.Error(fmt.Sprintf("put data info error；%s", err))
	}

	return shim.Success([]byte(reqId))
}

type CallData struct {
	ServiceName       string                         `json:"service_name"`
	CrossChainCode    string                         `json:"cc_code"`
	CallBackChaincode string                         `json:"cb_cc"`
	CrossData         *crosschaincode.FiscoBcosInput `json:"cross_data"`
}

func (c *SCChaincode) callFisco(stub shim.ChaincodeStubInterface, args []string) peer.Response {

	if len(args) < 1 {
		return shim.Error("Length cannot be less than 1")
	}

	callData := &CallData{}
	fmt.Println(args[0])

	err := json.Unmarshal([]byte(args[0]), callData)
	if err != nil {
		return shim.Error("args format failed ," + err.Error())
	}

	cb_fcn := "callback"

	reqId, err := crosschaincode.CallCrossService(stub, callData.ServiceName, callData.CrossChainCode, callData.CrossData, callData.CallBackChaincode, cb_fcn, 100)
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
