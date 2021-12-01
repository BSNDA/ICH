package hellopoly

import (
	"encoding/hex"
	"fmt"
	"github.com/hyperledger/fabric-chaincode-go/shim"
	"github.com/hyperledger/fabric-protos-go/peer"
	"time"
)

type HelloPoly struct {
}

func setLogger(logInfo ...interface{}) {
	t := time.Now()
	fmt.Printf("%s  ->  %s\n", t.Format("2006-01-02 15:04:05.000"), logInfo)
}

/**
 * @Author AndyCao
 * @Date 2020-11-4 18:27
 * @Description  Contract initialization
 * @Param
 * @Return
 **/
func (t *HelloPoly) Init(stub shim.ChaincodeStubInterface) peer.Response {
	setLogger("Start initializing init method......")
	defer setLogger("End initialization of init method......")
	return shim.Success(nil)
}

/**
 * @Author AndyCao
 * @Date 2020-11-4 18:27
 * @Description  Contract invocation
 * @Param
 * @Return
 **/
func (t *HelloPoly) Invoke(stub shim.ChaincodeStubInterface) peer.Response {
	function, args := stub.GetFunctionAndParameters()
	switch function {
	case "say":
		return t.say(stub, args)
	case "hear":
		return t.hear(stub, args)
	case "getValue":
		return t.getValue(stub, args)
	default:
		setLogger("Unavailable method!")
		break
	}
	return shim.Error("Unavailable request!")
}

/**
 * @Author AndyCao
 * @Date 2020-11-4 18:27
 * @Description This method is used for calling other target chains across the chain. (This method can be self-defined)
 * @Param
 * @Return
 **/
func (t *HelloPoly) say(stub shim.ChaincodeStubInterface, args []string) peer.Response {
	setLogger("The say method is called......")
	defer setLogger("End called say method......")
	// Check configurations
	if len(args) != 4 {
		return shim.Error("Parameter error!!!")
	}
	// Save data
	if err:=stub.PutState(args[1],[]byte(args[2]));err!=nil{
		return shim.Error(fmt.Sprintf("Failed to save data: %v", err))
	}
	// Build cross-chain management contract calling parameters
	invokeArgs := make([][]byte, 6)
	// Set the calling method of the cross-chain management contract
	invokeArgs[0] = []byte("crossChain")
	// Set the chain corresponding to the target chain in Poly network 
	invokeArgs[1] = []byte(args[0])
	// Set the target chain’s smart contract address, note：
	// 1. If the target chain is fabric, then this is the name of the application contract, for example：mycc; if the target chain is fisco/eth/neo, then this is the name of the application contract, for example: 11..., don't add "0x" prefix.
	// 2. The parameters passed to the cross-chain management contract must be EncodeToString converts bytes to hex strings and then transform to byte arrays.
	if args[0]=="8"||args[0]=="9"{
		invokeArgs[2] = []byte(hex.EncodeToString([]byte(args[1])))
	}else{
		invokeArgs[2] = []byte(args[1])
	}
        // Target chain’s application contract method
	invokeArgs[3] = []byte("hear")
	// Cross-chain information that target chain’s application contract needs to pass（Note: The parameters passed to the cross-chain management contract must be EncodeToString converts bytes to hex strings and then transform to byte arrays.）
	invokeArgs[4] = []byte(hex.EncodeToString([]byte(args[2])))
	// Name of the application contract
	invokeArgs[5] = []byte(args[3])
	// Calling cross-chain management contract
	var resp = stub.InvokeChaincode(string("ccm"), invokeArgs, "")
	if resp.Status != shim.OK {
		return shim.Error(fmt.Sprintf("Failed to call the cross-chain management contract %s: %s", "ccm", resp.Message))
	}
	// Set event notification
	if err := stub.SetEvent("from_ccm", resp.Payload); err != nil {
		return shim.Error(fmt.Sprintf("Event setting failed: %v", err))
	}
	setLogger(fmt.Sprintf("Successfully call the cross-chain management contract for cross-chain: (target chain ID: %d, target chain contract address: %x, cross-chain message: %s)", args[0], args[1], args[2]))
	return shim.Success(nil)
}

/**
 * @Author AndyCao
 * @Date 2020-11-4 18:28
 * @Description This method is used for calling other target chains across the chain. (This method can be self-defined)
 * @Param
 * @Return
 **/
func (t *HelloPoly) hear(stub shim.ChaincodeStubInterface, args []string) peer.Response {
	setLogger("The hear method is called......")
	defer setLogger("End called hear method......")
	// Check parameters
	if len(args) != 1 {
		return shim.Error("Parameter error!!!")
	}

	// Save the cross-chain information committed by the source chain
	if err:=stub.PutState("FABRIC_CROSS_CHAIN",[]byte(args[0]));err!=nil{
		return shim.Error(fmt.Sprintf("Failed to save data: %v", err))
	}
	return shim.Success([]byte("SUCCESS"))
}

/**
 * @Author AndyCao
 * @Date 2020-11-4 18:39
 * @Description  Get data
 * @Param
 * @Return
 **/
func (t *HelloPoly) getValue(stub shim.ChaincodeStubInterface, args []string) peer.Response {
	setLogger("The getValue method is called......")
	defer setLogger("End getValue hear method......")
	// Check parameters
	if len(args) != 1 {
		return shim.Error("Parameter error!!!")
	}

	// Get data
	bytes,err:=stub.GetState(args[0])
	if err!=nil{
		return shim.Error(fmt.Sprintf("Get data error : %v", err))
	}
	return shim.Success(bytes)
}
