package main

import (
	"fmt"
	"github.com/BSNDA/ICH/sample/irita/consumers/fabric/chaincode"
	"github.com/hyperledger/fabric/core/chaincode/shim"
)

func main() {
	err := shim.Start(new(chaincode.SCChaincode))
	if err != nil {
		fmt.Printf("Error starting SCChaincode: %s", err)
	}
}
