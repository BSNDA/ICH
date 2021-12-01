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
	case "send":
		return t.send(stub, args)
	case "set":
		return t.set(stub, args)
	case "get":
		return t.get(stub, args)
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
func (t *HelloPoly) send(stub shim.ChaincodeStubInterface, args []string) peer.Response {
	setLogger("The send method is called......")
	defer setLogger("End called send method......")
	// Check configurations
	if len(args) != 4 {
		return shim.Error("Parameter error!!!")
	}
	// Save data
	if err:=stub.PutState(args[1],[]byte(args[2]));err!=nil{
		return shim.Error(fmt.Sprintf("Failed to save data: %v", err))
	}
	// Build cross-chain management contract calling parameters
	// Parameter 1: Chain ID corresponding to the target chain
	// Parameter 2: the name or address of the target chain application contract 
	// (Note: the contract name of fabric must be converted into a hexadecimal string, such as hex.EncodeToString ([]byte("")), 
	// the contract address of FISCO and Eth needs to remove the prefix of 0x, and the contract address of Neo needs to remove the prefix of 0x, 
	// and the string is small terminated)
	// Parameter 3: define method corresponding to target chain application contract
	// Parameter 4: Cross chain information sent to the target chain
	ccArgs := []string{"crossChain",
		args[0],
		args[1],
		args[2],
		hex.EncodeToString([]byte(args[3]))}
	// Calling cross-chain management contract
	var resp = stub.InvokeChaincode(string("ccm"), packArgs(ccArgs), "")
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
func (t *HelloPoly) set(stub shim.ChaincodeStubInterface, args []string) peer.Response {
	setLogger("The set method is called......")
	defer setLogger("End called set method......")
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
func (t *HelloPoly) get(stub shim.ChaincodeStubInterface, args []string) peer.Response {
	setLogger("The get method is called......")
	defer setLogger("End get hear method......")
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

func packArgs(args []string) [][]byte {
	r := [][]byte{}
	for _, s := range args {
		temp := []byte(s)
		r = append(r, temp)
	}
	return r
}
