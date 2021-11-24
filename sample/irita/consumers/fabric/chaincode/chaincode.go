package chaincode

import (
	"encoding/json"
	"fmt"
	"github.com/hyperledger/fabric/core/chaincode/shim"
	"github.com/hyperledger/fabric/protos/peer"
	"strings"

	"github.com/BSNDA/ICH/sample/irita/consumers/fabric/crosschaincode"
)

var successMsg = []byte("success")

type CrossData struct {
	Id     string `json:"id"`
	Input  string `json:"input"`
	Output string `json:"output"`
}

func crossKey(id string) string {
	return "css_" + id
}

type SCChaincode struct {
}

func (c *SCChaincode) Init(stub shim.ChaincodeStubInterface) peer.Response {

	return shim.Success(successMsg)
}

func (c *SCChaincode) Invoke(stub shim.ChaincodeStubInterface) peer.Response {
	fmt.Println("chainCode Invoke")
	function, args := stub.GetFunctionAndParameters()

	if strings.ToLower(function) == "callchainlink" {
		return c.callNewChainLink(stub, args)
	}

	if strings.ToLower(function) == "callback" {
		return c.callback(stub, args)
	}

	if strings.ToLower(function) == "query" {
		return c.query(stub, args)
	}

	if strings.ToLower(function) == "callfabric" {
		return c.callFabric(stub, args)
	}

	if strings.ToLower(function) == "callfisco" {
		return c.callFisco(stub, args)
	}

	return shim.Error("function not found")
}

func (c *SCChaincode) callback(stub shim.ChaincodeStubInterface, args []string) peer.Response {

	output := args[0]
	fmt.Println("output:", output)
	res, err := crosschaincode.GetCallBackInfo(output)
	if err != nil {
		return shim.Error("error")
	}

	ser, err := stub.GetState(crossKey(res.RequestId))
	if err != nil || len(ser) == 0 {
		return shim.Error("the requestID invalid")
	}

	cd := &CrossData{}
	err = json.Unmarshal(ser, cd)
	if err != nil {
		return shim.Error("error")
	}
	cd.Output = res.Output
	cdb, _ := json.Marshal(cd)
	if err := stub.PutState(crossKey(res.RequestId), cdb); err != nil {
		return shim.Error(fmt.Sprintf("put data info error；%s", err))
	}
	return shim.Success(successMsg)
}

func (c *SCChaincode) query(stub shim.ChaincodeStubInterface, args []string) peer.Response {
	id := args[0]
	ser, err := stub.GetState(crossKey(id))
	if err != nil || len(ser) == 0 {
		return shim.Error("the requestID invalid")
	}
	return shim.Success(ser)
}
