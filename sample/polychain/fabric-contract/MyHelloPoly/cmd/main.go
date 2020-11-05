package main

import (
	"github.com/hyperledger/fabric/core/chaincode/shim"
	"github.com/polynetwork/fabric-contract/MyHelloPoly"
)

func main() {
	err := shim.Start(new(MyHelloPoly.HelloPoly))
	if err != nil {
		panic(err)
	}
}
