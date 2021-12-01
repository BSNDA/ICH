package chaincode

//nft input and out

type NftInput struct {
	ABIEncoded   string `json:"abi_encoded,omitempty"`
	To           string `json:"to"`
	AmountToMint string `json:"amount_to_mint"`
	MetaID       string `json:"meta_id"`
	SetPrice     string `json:"set_price"`
	IsForSale    bool   `json:"is_for_sale"`
}

type NftOutput struct {
	NftID string `json:"nft_id"`
}

type InputData struct {
	Header interface{} `json:"header"`
	Body   interface{} `json:"body"`
}

//fisco bcos store

type BcosInput struct {
	Value string `json:"value"`
}

type BcosOutput struct {
	Key string `json:"key"`
}

//["跨链合约名称","目标链Id","目标链请求类型","目标链合约名","目标链参数","回调合约名","回调合约方法"]
type CrossFabricReqest struct {
	CrossChaincodeName string `json:"ccn"`
	TargetChainId      string `json:"tcid"`
	TargetType         string `json:"tt"`

	TargetChaincodeName string `json:"tcn"`
	TargetArgs          string `json:"ta"`

	CallBackChaincodeName         string `json:"cbcn"`
	CallBackChaincodeFunctionName string `json:"cbcfn"`
}
