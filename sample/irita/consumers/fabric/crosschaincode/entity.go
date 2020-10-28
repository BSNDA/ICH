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
