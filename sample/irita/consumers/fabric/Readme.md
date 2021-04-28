# 基于Fabric 框架的跨链应用合约

## 应用合约开发说明
1. 开发准备：
需要首先获取BSN跨链消费合约的帮助包 目前只有go语言版本，后续会增加其他版本
```
cd $GOPATH
mkdir -p src/github.com/BSNDA && cd src/github.com/BSNDA
git clone https://github.com/BSNDA/ICH.git
```


2. 跨链调用
创建Fabric链码对象、以及Invoke方法后，引入包
```
import (
  "github.com/BSNDA/ICH/sample/irita/consumers/fabric/crosschaincode"
)
```
在invoke方法中直接调用 `crosschaincode.CallCrossService`方法，该方法的参数如下:
* stub : shim.ChaincodeStubInterface
* serviceName : 调用的跨链服务名称，通用联盟链跨链为 `cross_service`
* crossChaincode : 跨链管理合约地址  
* input : 跨链服务的输入对象，
* callbackCC ：回调的合约名称
* callbackFcn ：回调的合约方法名称
* timeout : 超时时间

其中 input 参数根据跨链调用的目标链类型不同，传入的类型不同
在以`fabric`为目标链的服务中`input`结构为`crosschaincode.FabricInput`
```
type FabricInput struct {
	ChainId     uint64   `json:"chainId"`
	ChainCode   string   `json:"chainCode"`
	FunType     string   `json:"funType"`
	Args        []string `json:"args"`
}
```
> `ChainId` 为调用的目标链的链ID，可以在BSN的应用详情页找到  
> `ChainCode` 为调用的目标链的合约名称  
> `FunType` 为调用的目标链的方法类型，可选为`query`和`invoke`  
> `Args` 为调用的目标链的参数，其中第一个参数为调用的合约方法名，其他为参数  

在以`FISCO-BCOS`为目标链的服务中`input`结构为`crosschaincode.FiscoBcosInput`
```
type FiscoBcosInput struct {
	OptType         string `json:"optType"`
	ChainId         uint64  `json:"chainId"`
	ContractAddress string `json:"contractAddress"`
	CallData        string `json:"callData"`
}
```
> `OptType`为调用的合约的方法类型，其中`constant`为`call`,`非constant`为`tx`  
> `ChainId` 为调用的目标链的链ID，可以在BSN的应用详情页找到  
> `ContractAddress` 为调用的目标合约的合约地址  
> `CallData` 为使用调用的目标合约的合约ABI、合约方法名、合约参数、序列化后的数据的哈希字符串，不包含`0x`,可以参考BSN网关go语言SDK的 [ParesData](https://github.com/BSNDA/PCNGateway-Go-SDK/blob/6d97d885f96597f4b35040df17fdca1fbcda07ab/pkg/core/trans/fiscobcos/trans.go#L24)  

调用成功，将返回唯一的请求ID，请注意保存该值，在回调方法中可以根据该值判断跨链结果。

3. 结果回调
跨链合约在收到relayer返回的跨链交易结果后，将会调用跨链交易时传入的回调合约名以及方法名返回跨链结果信息，
其中调用的第一个参数为返回信息，返回值为`JSON`的字符串，格式为：
```
type ServiceResponse struct {
    RequestId   string `json:"requestID,omitempty"`
    ErrMsg      string `json:"errMsg,omitempty"`
    Output      string `json:"output,omitempty"`
    IcRequestId string `json:"icRequestID,omitempty"`
}
```
也可以使用`crosschaincode.GetCallBackInfo()` 序列化该值，其中`RequestId` 为调用跨链合约成功时返回的唯一请求ID，
可以根据该字段进行相关业务处理。
`Output` 为跨链结果返回值，该字段为`JSON`格式的字符串，格式如下：
```
type InputData struct {
    Header interface{} `json:"header"`
    Body interface{}	`json:"body"`
}
```
其中 `Body` 为对应服务的输出对象，

在以`Fabric`为目标链的调用中 `output`结构如下
```
type FabricOutput struct {
	TxValidationCode int32  `json:"txValidationCode"`
	ChaincodeStatus  int32  `json:"chaincodeStatus"`
	TxId             string `json:"txId"`
	Payload          string `json:"payload"`
}
```
> `TxValidationCode` 为交易的状态  
> `ChaincodeStatus`为合约的执行状态  
> `TxId`为交易ID  
> `Payload` 为合约的返回结果的哈希字符串  

在以`FISCO-BCOS`为目标链的调用中`output`结构如下
```
type FiscoBcosOutput struct {
	Result string `json:"result,omitempty"`
	Status bool   `json:"status,omitempty"`
	TxHash string `json:"tx_hash,omitempty"`
}
```
> `Result` 为合约的返回结果，调用的方法为`call`时，有值  
> `Status` 合约的执行结果状态  
> `TxHash` 为合约的执行交易哈希，调用的方法为`tx`时，有值  

4. 链码打包
由于该链码引用了外部的包，需要在打包时将外部包同时打包，可以使用`govendor`打包
安装 `govendor`
```
go get -u -v github.com/kardianos/govendor
```
在项目的`main`方法目录中执行
```
govendor init
govendor add -tree github.com/BSNDA/ICH/sample/irita/consumers/fabric/crosschaincode
```
最后将项目以及`vendor`目录压缩，在BSN门户上传合约包，进行部署。

打包部署注意事项：
> 在`main.go`所在的目录压缩文件，仅支持`zip`格式,例如本实例中的[fabric.zip](https://github.com/BSNDA/ICH/blob/main/sample/irita/consumers/fabric/fabric.zip)，文件可重新命名  
> `main`函数路径为相对于`src`的路径，本实例中为`github.com/BSNDA/ICH/sample/irita/consumers/fabric`  
> 本实例中由于`crosschaincode`非外部包，所以没有使用`govendor`  
