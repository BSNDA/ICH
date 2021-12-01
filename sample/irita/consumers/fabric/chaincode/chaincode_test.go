package chaincode

import (
	"encoding/json"
	"fmt"
	"testing"
)

func TestCallChainLink(t *testing.T) {
	data := "{\"service_name\":\"eth-usdt-price\",\"providers\":[\"iaa16eu2jvgpa5ek9mn2tn985jlm89e6dch92qwj09\"],\"input\":\"{\\\"header\\\":{},\\\"body\\\":{}}\",\"timeout\":100,\"service_fee_cap\":\"1000000upoint\"}"

	req := &CrossFabricReqest{
		CrossChaincodeName:            "cc_cross",
		TargetChainId:                 "10227431719070003",
		TargetChaincodeName:           "iaa15s9sulrnmctzluc42g7lkxh92ardkc9xccxsy9",
		TargetType:                    "service",
		TargetArgs:                    data,
		CallBackChaincodeFunctionName: "callback",
		CallBackChaincodeName:         "cc_cross_consumers",
	}

	jb, _ := json.Marshal(req)

	var as []string
	as = append(as, "callchainlink")
	as = append(as, string(jb))
	jbs, _ := json.Marshal(&as)
	fmt.Println(string(jbs))
}
