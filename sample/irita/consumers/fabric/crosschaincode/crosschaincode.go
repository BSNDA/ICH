package crosschaincode

import (
	"encoding/json"
	"errors"
	"fmt"
	"github.com/hyperledger/fabric/core/chaincode/shim"
	"strings"
)

const (
	CrossChaincode     = "cc_cross"
	CrossChainCodeFunc = "sendrequest"

	EndpointType_Call    = "contract_query"
	EndpointType_Tx      = "contract_tx"
	EndpointType_Service = "service"
)

func CallService(stub shim.ChaincodeStubInterface, serviceName string, input interface{}, callbackCC, callbackFcn string, timeout uint64) (string, error) {

	data := &InputData{
		Header: struct{}{},
		Body:   input,
	}
	dataBytes, _ := json.Marshal(data)

	req := &ServiceRequest{
		ServiceName: serviceName,
		Input:       string(dataBytes),
		Timeout:     timeout,
	}

	if strings.TrimSpace(callbackCC) != "" && strings.TrimSpace(callbackFcn) != "" {
		req.CallBack = &CallBackInfo{
			ChainCode: callbackCC,
			FuncName:  callbackFcn,
		}
	}

	b, _ := json.Marshal(req)

	var args [][]byte
	args = append(args, []byte("callservice"))
	args = append(args, b)

	res := stub.InvokeChaincode(CrossChaincode, args, "")
	txId := string(res.Payload)
	//stub.SetEvent(fmt.Sprintf("CallService_%s", txId), []byte(txId))
	return txId, nil
}

//

func CallCrossService(stub shim.ChaincodeStubInterface,

	crossChaincode string,

	targetChainId string,
	targetChainCode string,

	callType string,
	callArgs string,

	callbackCC,
	callbackFcn string) (string, error) {

	var args [][]byte
	args = append(args, []byte(CrossChainCodeFunc))

	args = append(args, getEndpointInfo(targetChainId, targetChainCode, callType))
	args = append(args, []byte(callType))
	args = append(args, []byte(callArgs))
	args = append(args, []byte(callbackCC))
	args = append(args, []byte(callbackFcn))

	res := stub.InvokeChaincode(crossChaincode, args, "")
	fmt.Println("res.Status:", res.Status)
	fmt.Println("res.Message:", res.Message)
	txId := string(res.Payload)
	fmt.Println("res.Payload:", txId)
	//stub.SetEvent(fmt.Sprintf("CallService_%s", txId), []byte(txId))
	return txId, nil
}

func getEndpointInfo(chainId string, chaincode, endpointType string) []byte {
	ei := &EndpointInfo{
		DestChainID:     chainId,
		EndpointAddress: chaincode,
		EndpointType:    endpointType,
		DestChainType:   endpointType,
	}

	eiBytes, _ := json.Marshal(ei)
	return eiBytes
}

func GetParameters(allargs []string) []byte {

	argsBytes, _ := json.Marshal(&allargs)

	return argsBytes
}

func GetCallBackInfo(output string) (*ServiceResponse, error) {
	fmt.Println("output:", output)
	res := &ServiceResponse{}
	err := json.Unmarshal([]byte(output), res)
	if err != nil {
		return nil, errors.New("error")
	}
	return res, nil
}
