package chaincode

import (
	"fmt"
	"github.com/hyperledger/fabric/core/chaincode/shim"
	"github.com/hyperledger/fabric/protos/peer"
	"strings"
)

var successMsg = []byte("success")

func StoreKey(key string) string {

	return fmt.Sprintf("store_%s", key)
}

type StoreChainCode struct {
}

func (c *StoreChainCode) Init(stub shim.ChaincodeStubInterface) peer.Response {

	return shim.Success(successMsg)
}

func (c *StoreChainCode) Invoke(stub shim.ChaincodeStubInterface) peer.Response {
	LogInfo("chainCode Invoke")
	function, args := stub.GetFunctionAndParameters()

	if strings.ToLower(function) == "save" {
		return c.save(stub, args)
	}

	if strings.ToLower(function) == "delete" {
		return c.delete(stub, args)
	}

	if strings.ToLower(function) == "query" {
		return c.query(stub, args)
	}

	return shim.Error("function not found")
}

func (c *StoreChainCode) save(stub shim.ChaincodeStubInterface, args []string) peer.Response {

	if len(args) < 2 {
		return shim.Error("Length cannot be less than 2")
	}

	key := args[0]
	value := []byte(args[1])
	LogInfo("Save.Key : %s", key)
	LogInfo("Save.Value :%s", args[1])
	if err := stub.PutState(StoreKey(key), value); err != nil {
		return shim.Error("Save Has Error")
	}

	stub.SetEvent(fmt.Sprintf("save_%s", key), value)

	return shim.Success(successMsg)
}

func (c *StoreChainCode) delete(stub shim.ChaincodeStubInterface, args []string) peer.Response {
	if len(args) < 1 {
		return shim.Error("Length cannot be less than 2")
	}

	key := args[0]
	LogInfo("Delete.Key : %s", key)
	if err := stub.DelState(StoreKey(key)); err != nil {
		return shim.Error("Delete Has Error")
	}

	stub.SetEvent(fmt.Sprintf("delete_%s", key), []byte(key))

	return shim.Success(successMsg)
}

func (c *StoreChainCode) query(stub shim.ChaincodeStubInterface, args []string) peer.Response {
	if len(args) < 1 {
		return shim.Error("Length cannot be less than 2")
	}

	key := args[0]

	LogInfo("Query.Key : %s", key)

	value, err := stub.GetState(StoreKey(key))
	if err != nil {
		return shim.Error("Delete Has Error : key not exist")
	}

	LogInfo("Query.Value : %s", string(value))
	return shim.Success(value)
}
