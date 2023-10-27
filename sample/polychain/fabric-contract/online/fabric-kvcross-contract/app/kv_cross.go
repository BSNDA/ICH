package app

import (
	"encoding/hex"
	"fmt"
	"github.com/hyperledger/fabric-chaincode-go/shim"
	"github.com/hyperledger/fabric-protos-go/peer"
	"strings"
)

const (
	// Cross-chain message dissemination method of cross-chain management contract
	CROSS_CHAIN    = "crossChain"
	CHAINID_PREFIX = "chainid_"
)

// The first step is to call the bind method, which only needs to be called once
// The second step is to call the send method

type KVCross struct {
}

func (user *KVCross) Init(stub shim.ChaincodeStubInterface) peer.Response {

	return shim.Success(nil)
}

func (user *KVCross) Invoke(stub shim.ChaincodeStubInterface) peer.Response {

	function, _ := stub.GetFunctionAndParameters()
	args := stub.GetArgs()
	if len(args) == 0 {
		return shim.Error("no args")
	}
	args = args[1:]

	switch function {
	case "bind":
		return user.bind(stub, args)
	case "send":
		return user.send(stub, args)
	case "set":
		return user.set(stub, args)
	case "get":
		return user.get(stub, args)
	}

	return shim.Error("Invalid invoke function name. Expecting \"set\" \"get\" ")
}

/**
 * @description: Bind the target chain id and the target chain application contract
 * @param: toChainId、toCcName
 * @return:
 */
func (user *KVCross) bind(stub shim.ChaincodeStubInterface, args [][]byte) peer.Response {
	// Chain id of the target chain
	toChainId := string(args[0])

	// Get the application contract of the target chain
	toCcName := args[1]

	if err := stub.PutState(CHAINID_PREFIX+toChainId, toCcName); err != nil {
		return shim.Error(err.Error())
	}
	return shim.Success(nil)
}

/**
 * @description: Call the cross-chain management contract and send cross-chain transactions
 * @param: ccmName、toChainId、toCcFuncName、toCrossArgs
 * @return:
 */
func (user *KVCross) send(stub shim.ChaincodeStubInterface, args [][]byte) peer.Response {
	if len(args) != 4 {
		return shim.Error("require four args")
	}

	// The name of the original chain's cross-chain management contract
	ccmName := string(args[0])

	// Chain id of the target chain
	toChainId := string(args[1])

	// Get the application contract name of the target chain
	toCcName, err := stub.GetState(CHAINID_PREFIX + toChainId)
	if err != nil {
		return shim.Error(err.Error())
	}

	if len(toCcName) == 0 || string(toCcName) == "" {
		return shim.Error("not bind toChainId with toCcName")
	}

	// The method name of the application contract of the target chain. The contract can be setget
	toCcFuncName := string(args[2])
	if toCcFuncName == "" {
		return shim.Error("toCcFuncName is empty")
	}

	// Cross-chain information
	toCrossArgs := string(args[3])

	ccArgs := []string{CROSS_CHAIN,
		toChainId,
		string(toCcName),
		toCcFuncName,
		hex.EncodeToString([]byte(toCrossArgs))}

	// Call the cross Chain method of this channel's cross-chain management contract
	resp := stub.InvokeChaincode(ccmName, packArgs(ccArgs), "")

	if resp.Status != 200 {
		return shim.Error(fmt.Sprintf("cross chain %s fail : %s", toChainId, resp.String()))
	}

	// Set event notification
	if err := stub.SetEvent("from_ccm", resp.Payload); err != nil {
		return shim.Error(fmt.Sprintf("Event setting failed: %v", err))
	}

	fmt.Println(fmt.Sprintf("Successfully call the cross-chain management contract for cross-chain: (target chain ID: %d, target chain contract address: %x, cross-chain message: %s)", args[0], args[1], args[2]))

	return resp
}

func (user *KVCross) set(stub shim.ChaincodeStubInterface, args [][]byte) peer.Response {

	arg, _ := hex.DecodeString(string(args[0]))
	fmt.Println("arg: ", string(arg))
	temp := strings.Split(string(arg), ",")

	if len(temp) != 2 {
		return shim.Error(fmt.Sprintf("arg error : %s", arg))
	}

	key := temp[0]

	value := []byte(temp[1])

	if err := stub.PutState(key, value); err != nil {
		return shim.Error(err.Error())
	}

	res, err := stub.GetState(key)
	if err != nil {
		return shim.Error(err.Error())
	}

	fmt.Println(fmt.Sprintf("success set key: %s, value: %s", key, string(res)))

	return shim.Success(nil)
}

func (user *KVCross) get(stub shim.ChaincodeStubInterface, args [][]byte) peer.Response {
	if len(args) != 1 {
		return shim.Error("require one args")
	}

	key := args[0]

	value, err := stub.GetState(string(key))

	if err != nil {
		return shim.Error(err.Error())
	}

	fmt.Println(fmt.Sprintf("success get key: %s, value: %s", string(key), string(value)))

	return shim.Success(value)
}

func packArgs(args []string) [][]byte {
	r := [][]byte{}
	for _, s := range args {
		temp := []byte(s)
		r = append(r, temp)
	}
	return r
}
