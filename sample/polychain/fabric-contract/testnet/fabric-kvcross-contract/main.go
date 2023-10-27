package main

import (
	"fabric-kvcross-contract/app"
	"github.com/hyperledger/fabric-chaincode-go/shim"
)

func main() {

	err := shim.Start(new(app.KVCross))
	if err != nil {
		panic(err)
	}
}
