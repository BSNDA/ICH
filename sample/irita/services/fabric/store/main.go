package main

import (
	"fmt"
	"github.com/BSNDA/ICH/sample/irita/services/fabric/store/chaincode"
	"github.com/hyperledger/fabric/core/chaincode/shim"
)

func main() {

	err := shim.Start(new(chaincode.StoreChainCode))
	if err != nil {
		fmt.Printf("Error starting StoreChainCode: %s", err)
	}
}
