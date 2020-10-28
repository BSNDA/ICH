# iService 消费合约开发文档

消费合约是 iService (Interchain Service) 跨链服务架构中由开发者编写的业务合约，通过调用部署在各应用链(如 Ethereum, Hyperledger Fabric, FISCO BCOS 等) 上的 iService 消费者代理合约（iService Core Extension）发起跨链请求。

本文档介绍如何用 `Solidity` 开发基于 iService 的跨链消费合约。适用于 `EVM` 兼容的应用链平台，如 Ethereum、FISCO BCOS 等。

## iService Client

为方便开发者，我们提供了 `iService Client` 合约。iService Client 封装了与 iService 核心代理合约的交互，并实现了事件触发、请求校验、状态维护等功能，可以帮助开发人员快速进行开发。

`iService Client` 合约在[示例合约代码](#示例代码)中可以找到。

## 开发流程

### 导入 iService Client

  导入本地的 iService Client 合约，例如：

  ```bash
  import iServiceClient.sol
  ```

  _提示：_ 也可以直接将 iService Client 合约代码作为消费合约的一部分。

### 继承 iService Client

  ```bash
  contract <consuming-contract-name> is iServiceClient {
  }
  ```

### 实现 iService 调用

- 设置 iService Core Extenstion 即 iService 核心代理合约地址

    通过调用继承于 iService Client 的方法 `setIServiceCore(address _iServiceCore)` 即可。或者在合约构造函数中传入。如下所示：

    ```bash
    constructor(
        address _iServiceCore
    )
        public
    {
        setIServiceCore(_iServiceCore);
    }
    ```

- 实现回调方法

    当 iService 核心代理合约接收到跨链调用响应时，将调用消费合约的回调函数回传结果。

    回调接口为：

    ```bash
    function callback(
        bytes32 _requestID,
        string calldata _output
    )
    ```

- 发起 iService 调用

    通过调用继承于 iService Client 的 `sendIServiceRequest` 即可发起跨链服务调用：

    ```bash
    bytes32 memory requestID = sendIServiceRequest(
            serviceName,
            requestInput,
            timeout,
            address(this),
            this.callback.selector
        );
    ```

## NFT 服务消费合约示例

NFT 服务由部署在 Ethereum Ropsten 测试网上的 [NFT 合约](http://ropsten.etherscan.io/address/0x80f2a29e861a1888603b6bbd54453ee995c808ad) 提供。此合约用于创建 NFT 资产。

开发者可以在应用链上开发相应的消费合约实现跨链创建 NFT 资产。
