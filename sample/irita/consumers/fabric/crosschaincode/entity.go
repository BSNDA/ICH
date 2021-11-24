package crosschaincode

type InputData struct {
	Header interface{} `json:"header"`
	Body   interface{} `json:"body"`
}

type ServiceRequest struct {
	RequestId   string        `json:"requestID,omitempty"`   //服务请求 ID  本合约中使用 合约交易ID
	ServiceName string        `json:"serviceName,omitempty"` //服务定义名称
	Input       string        `json:"input,omitempty"`       //服务请求输入；需符合服务的输入规范
	Timeout     uint64        `json:"timeout,omitempty"`     //请求超时时间；在目标链上等待的最大区块数
	CallBack    *CallBackInfo `json:"callback,omitempty"`    //回调的合约以及方法
}

type CallBackInfo struct {
	ChainCode string `json:"chainCode"`
	FuncName  string `json:"funcName"`
}
type ServiceResponse struct {
	RequestId   string `json:"requestID,omitempty"` //服务请求 ID  本合约中使用 合约交易ID
	ErrMsg      string `json:"errMsg,omitempty"`
	Output      string `json:"output,omitempty"`
	IcRequestId string `json:"icRequestID,omitempty"`
}

//bsn IRITA跨链中,标链为 fabric的标准输入结构
type FabricInput struct {
	ChainId   uint64   `json:"chainId"`
	ChainCode string   `json:"chainCode"`
	FunType   string   `json:"funType"`
	Args      []string `json:"args"`
}

//bsn IRITA跨链中,目标链为 FISCO-BCOS的标准输入结构
type FiscoBcosInput struct {
	OptType         string `json:"optType"`
	ChainID         uint64 `json:"chainId"`
	ContractAddress string `json:"contractAddress"`
	CallData        string `json:"callData"`
}

type FabricOutput struct {
	TxValidationCode int32  `json:"txValidationCode"`
	ChaincodeStatus  int32  `json:"chaincodeStatus"`
	TxId             string `json:"txId"`
	Payload          string `json:"payload"`
}

type FiscoBcosOutput struct {
	Result string `json:"result,omitempty"`
	Status bool   `json:"status,omitempty"`
	TxHash string `json:"tx_hash,omitempty"`
}

type EndpointInfo struct {
	DestSubChainID  string `json:"dest_sub_chain_id"`
	DestChainID     string `json:"dest_chain_id"`
	DestChainType   string `json:"dest_chain_type"`
	EndpointAddress string `json:"endpoint_address"`
	EndpointType    string `json:"endpoint_type"`
}
