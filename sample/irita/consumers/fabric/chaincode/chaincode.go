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

	if strings.ToLower(function) == "callnft" {
		return c.callNFT(stub, args)
	}

	if strings.ToLower(function) == "callstore" {
		return c.callFiscoStore(stub, args)
	}

	if strings.ToLower(function) == "callback" {
		return c.callback(stub, args)
	}

	if strings.ToLower(function) == "query" {
		return c.query(stub, args)
	}

	return shim.Error("function not found")
}

func (c *SCChaincode) callNFT(stub shim.ChaincodeStubInterface, args []string) peer.Response {
	serviceName := "nft"

	if len(args) == 0 {
		return shim.Error("the args cannot be empty")
	}

	input := &NftInput{}
	err := json.Unmarshal([]byte(args[0]), input)
	if err != nil {
		return shim.Error("the args[0] Unmarshal failed")
	}
	cb_cc := ""
	cb_fcn := "callback"

	if len(args) >= 2 {
		cb_cc = args[1]
	}
	if len(args) >= 3 {
		cb_fcn = args[2]
	}
	fmt.Println(cb_cc, cb_fcn)
	reqId, err := crosschaincode.CallService(stub, serviceName, input, cb_cc, cb_fcn, 100)
	if err != nil {
		return shim.Error("callNFT has failed ," + err.Error())
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

func (c *SCChaincode) callFiscoStore(stub shim.ChaincodeStubInterface, args []string) peer.Response {
	serviceName := "bcos-store"
	if len(args) == 0 {
		return shim.Error("the args cannot be empty")
	}

	input := &BcosInput{}
	err := json.Unmarshal([]byte(args[0]), input)
	if err != nil {
		return shim.Error("the args[0] Unmarshal failed")
	}

	cb_cc := ""
	cb_fcn := "callback"

	if len(args) >= 2 {
		cb_cc = args[1]
	}
	if len(args) >= 3 {
		cb_fcn = args[2]
	}
	fmt.Println(cb_cc, cb_fcn)
	reqId, err := crosschaincode.CallService(stub, serviceName, input, cb_cc, cb_fcn, 100)
	if err != nil {
		fmt.Println(err.Error())
		return shim.Error("callNFT has failed ," + err.Error())
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
