#### 基于Fabric 框架的跨链应用合约

##### 应用合约开发说明
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
在invoke方法中直接调用 `crosschaincode.CallService`方法，该方法的参数如下:
* stub : shim.ChaincodeStubInterface
* serviceName : 调用的跨链服务名称，ETH的为 `nft` ，FISCO BCOS 为`bcos-store`
* input : 跨链服务的输入对象，
* callbackCC ：回调的合约名称
* callbackFcn ：回调的合约方法名称
* timeout : 超时时间

其中 input 参数根据跨链服务不同，传入的类型不同
在ETH 的服务中`input`结构如下
```
type Input struct {
    ABIEncoded   string `json:"abi_encoded,omitempty"`
    To           string `json:"to"`
    AmountToMint string `json:"amount_to_mint"`
    MetaID       string `json:"meta_id"`
    SetPrice     string `json:"set_price"`
    IsForSale    bool   `json:"is_for_sale"`
}
```
在FISCO BCOS 服务中`input`结构如下
```
type BcosInput struct {
    Value string `json:"value"`
}
```

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

在ETH 的服务中`output`结构如下
```
type Output struct {
    NftID string `json:"nft_id"`
}
```
在FISCO BCOS 服务中`output`结构如下
```
type BcosOutput struct {
    Key string `json:"key"`
}
```

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