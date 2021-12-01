package main

import (
	"fmt"
	"github.com/BSNDA/ICH/sample/polychain/fabric-contract/testnet/hellopoly"
	"github.com/hyperledger/fabric-chaincode-go/shim"
)

func main() {
	err := shim.Start(new(hellopoly.HelloPoly))
	if err != nil {
		fmt.Printf("Error starting bcccode: %s", err)
	}
}
