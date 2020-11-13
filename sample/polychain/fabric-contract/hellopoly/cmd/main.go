package main

import (
	"fabric-contract/hellopoly"
	"fmt"
	"github.com/hyperledger/fabric/core/chaincode/shim"
)

func main() {
	err := shim.Start(new(hellopoly.HelloPoly))
	if err != nil {
		fmt.Printf("Error starting bcccode: %s", err)
	}
}
