<h1 align=center> Fabric Cross Chain ChainCode </h1>

# 1. 介绍

这里介绍Poly跨链的Fabric链码，包含跨链核心协议实现和资产跨链应用。本代码仅适合Fabric1.4，其他版本未经测试。核心协议实现*CrossChainManager*在ccm目录，实现了同步Poly创世区块头、更新Poly验证人、验证并传递跨链消息等功能。资产跨链应用是Poly资产跨链DApp的一部分，是Fabric链的实现，在assets目录下，它实现了ERC20的所有功能，同时兼具跨链的功能，在实例化链码时可以选择是都启动跨链功能。

# 2. 接口

## 2.1. 管理合约

### 2.1.1. 安装

以下例子基于[basic-network](https://github.com/hyperledger/fabric-samples/tree/release-1.4/basic-network)。

使用**fabric-tools**，启动一个客户端容器，便于安装链码：

```
cd commercial-paper/organization/magnetocorp/configuration/cli/
docker-compose -f docker-compose.yml up -d cliMagnetoCorp
```

首先在你的链码路径`fabric-samples/commercial-paper/organization/magnetocorp`下创建文件夹polynetwork，进入文件夹，下载代码：

```
mkdir polynetwork
cd polynetwork
git clone https://github.com/polynetwork/fabric-contract.git
```

下载后，安装链码到peer，正常返回200，然后实例化ccm：

```
docker exec cliMagnetoCorp peer chaincode install -n ccm -v 0 -p github.com/polynetwork/fabric-contract/ccm/cmd
docker exec cliMagnetoCorp peer chaincode instantiate -n ccm -v 0 -c '{"Args":["7"]}' -C mychannel
```

链码*CrossChainManager*的Init函数如下：

```go
func (manager *CrossChainManager) Init(stub shim.ChaincodeStubInterface) peer.Response {
	args := stub.GetArgs()
	if len(args) != 1 {
		return shim.Error("wrong length of args")
	}
	chainId, err := strconv.ParseUint(string(args[0]), 10, 64)
	if err != nil {
		return shim.Error(fmt.Sprintf("failed to parse chainId: %v", err))
	}
	rawChainId := make([]byte, 8)
	binary.LittleEndian.PutUint64(rawChainId, chainId)
	if err := stub.PutState(FabricChainID, rawChainId); err != nil {
		return shim.Error(fmt.Sprintf("failed to put deployer: %v", err))
	}

	op, err := utils.GetMsgSenderAddress(stub)
	if err != nil {
		return shim.Error(fmt.Sprintf("failed to get tx sender: %v", err))
	}
	if err = stub.PutState(CrossChainManagerDeployer, op.Bytes()); err != nil {
		return shim.Error(fmt.Sprintf("failed to put deployer: %v", err))
	}
	zero := big.NewInt(0)
	if err = stub.PutState(CrossChainId, zero.Bytes()); err != nil {
		return shim.Error(fmt.Sprintf("failed to put cross chain id zero: %v", err))
	}
	return shim.Success(nil)
}
```

仅需要一个参数即可，"7"是string类型，代表Fabric当前channel的跨链chainID，每个channel的跨链chainID不可相同，一条区块链对应一个ID。

### 2.1.2. 初始化

在跨链开始前，需要将Poly的关键区块头同步到ccm：

```
docker exec cliMagnetoCorp peer chaincode invoke -n ccm -c '{"Args":["initGenesisBlock", "00000000db056...9690f733d95a58bbd940000"]}' -C mychannel
```

如此，便在ccm中初始化了Poly的共识节点公钥，可以用来验证共识签名，以确保跨链的正确性。

如果poly的共识节点更改了，那么仅需同步对应的关键区块头即可：

```
docker exec cliMagnetoCorp peer chaincode invoke -n ccm -c '{"Args":["changeBookKeeper", "00000000db056d...e14a00f0494af56342e9c"]}' -C mychannel
```

### 2.1.3 调用函数

- **crossChain**

函数crossChain负责处理Fabric跨链应用的消息，是消息离开Fabric的出口，所有的跨链应用都需要调用ccm的crossChain，来把要跨链的消息传播出去：

```
docker exec cliMagnetoCorp peer chaincode invoke -n ccm -c '{"Args":["crossChain", "2", "D8aE73e06552E270340b63A8bcAbf9277a1aac99", "unlock", "cross_chain_msg_in_hex", "app_chaincode_name"]}' -C mychannel
```

参数包括：

”crossChain“为方法名；

”2“为目标链的chainID；

”D8aE73e06552E270340b63A8bcAbf9277a1aac99“为目标链的应用合约；

”unlock“为要调用的目标链合约的方法名；

”cross_chain_msg_in_hex“为应用链码要传递的跨链信息；

”app_chaincode_name“是应用链码的名字；

- **verifyHeaderAndExecuteTx**

该函数用于接收relayer转发的消息，这个消息是从其他链跨链到这个channel的。

```
docker exec cliMagnetoCorp peer chaincode invoke -n ccm -c '{"Args":["verifyHeaderAndExecuteTx", "merkle_proof_for_state", "header_to_verify_proof", "proof_for_header", "anchorHeader_to_verify_headrproof"]}' -C mychannel
```

参数包括：

“merkle_proof_for_state”：用来证明Poly交易状态的merkle proof；

“header_to_verify_proof”：区块头中的stateroot可以用来验证proof，header可以通过验证共识签名确保正确性；

“proof_for_header”：如果上一个header不是当前链码存储的验证人，即链码更新了验证人，header仅有上个周期的签名，需要额外提交一个proof，来证明该header有效；

“anchorHeader_to_verify_headrproof”：锚区块头是用来证明proof_for_header有效性的，它是当前周期的区块头；

- **getPolyEpochHeight**

从链码取当前同步的Poly的周期切换高度；

```
docker exec cliMagnetoCorp peer chaincode invoke -n ccm -c '{"Args":["getPolyEpochHeight"]}' -C mychannel
```

- **isAlreadyDone**

输入Poly交易hash，询问这个跨链消息是否在链码上处理过了；

```
docker exec cliMagnetoCorp peer chaincode invoke -n ccm -c '{"Args":["isAlreadyDone", "txhash_in_hex"]}' -C mychannel
```

- **getPolyConsensusPeers**

获取当前共识节点；

```
docker exec cliMagnetoCorp peer chaincode invoke -n ccm -c '{"Args":["getPolyConsensusPeers"]}' -C mychannel
```

## 2.2. 资产跨链应用

这里的资产跨链将资产逻辑和代理逻辑结合到了一起，和以太坊的实现不同，每一个资产单独对应一个链码。

### 2.2.1. 安装

类似于ccm，安装peth，peth为ETH在当前channel的资产映射，实现了所有ERC20的功能：

```
docker exec cliMagnetoCorp peer chaincode install -n peth -v 0 -p github.com/polynetwork/fabric-contract/assets/cmd
docker exec cliMagnetoCorp peer chaincode instantiate -n peth -v 0 -c '{"Args":["poly_eth", "pEth", "18", "1000000000000000000000000000", "peth", "true"]}' -C mychannel
```

Init的参数从左到右为：token name、symbol、decimal、totalsupply、chaincodeName、isLockProxy；

isLockProxy设置为“true”则开启跨链功能，所有参数均为string；

### 2.2.2. 初始化

设置管理链码名字：

```
docker exec cliMagnetoCorp peer chaincode invoke -n peth -c '{"Args":["setManager", "ccm"]}' -C mychannel
```

配置DApp，绑定目标链的合约hash，即LockProxy：

```
docker exec cliMagnetoCorp peer chaincode invoke -n peth -c '{"Args":["bindProxyHash", "2", "2EEA349947f93c3B9b74FBcf141e102ADD510eCE"]}' -C mychannel
```

参数“2”为目标链chainID，“2EEA349947f93c3B9b74FBcf141e102ADD510eCE”为LockProxy合约hash。

配置目标链资产hash，如下配置ETH：

```
docker exec cliMagnetoCorp peer chaincode invoke -n peth -c '{"Args":["bindAssetHash", "2", "0000000000000000000000000000000000000000"]}' -C mychannel
```

### 2.2.3 调用函数

关于账户地址我们使用了发送交易者的公钥信息，实现了一个地址逻辑：

```go
func GetMsgSenderAddress(stub shim.ChaincodeStubInterface) (common.Address, error) {
	creatorByte, err := stub.GetCreator()
	if err != nil {
		return common.Address{}, err
	}
	certStart := bytes.Index(creatorByte, []byte("-----BEGIN"))
	if certStart == -1 {
		return common.Address{}, fmt.Errorf("no CA found")
	}
	certText := creatorByte[certStart:]
	bl, _ := pem.Decode(certText)
	if bl == nil {
		return common.Address{}, fmt.Errorf("failed to decode pem")
	}

	cert, err := x509.ParseCertificate(bl.Bytes)
	if err != nil {
		return common.Address{}, fmt.Errorf("failed to parse CA: %v", err)
	}
	hash := sha256.New()
	hash.Write(cert.RawSubjectPublicKeyInfo)
	addr := common.BytesToAddress(hash.Sum(nil)[12:])
	return addr, nil
}
```

要获得自己的地址可以通过调用：

```
docker exec cliMagnetoCorp peer chaincode invoke -n peth -c '{"Args":["getMyAddr"]}' -C mychannel
```

结果为：

```
2020-10-29 09:04:05.923 UTC [chaincodeCmd] InitCmdFactory -> INFO 001 Retrieved channel (mychannel) orderer endpoint: orderer.example.com:7050
2020-10-29 09:04:05.931 UTC [chaincodeCmd] chaincodeInvokeOrQuery -> INFO 002 Chaincode invoke successful. result: status:200 payload:"\233X&&<\036I\234\374L\022\333\216\351\212\301\367XA\027" 
```

可以转换为：

```go
fmt.Println(hex.EncodeToString([]byte("\233X&&<\036I\234\374L\022\333\216\351\212\301\367XA\027")))
```

可以得到地址`9b5826263c1e499cfc4c12db8ee98ac1f7584117`。

跨链部分：

- **unlock**

该方法仅由管理合约调用，即链码ccm，会释放peth给指定账户。

参数仅有一个，就是跨链信息。

- **lock**

用户调用lock，锁定资产，即peth到链码地址。参数包括：目标链ID、目标链地址、金额。

```
docker exec cliMagnetoCorp peer chaincode invoke -n peth -c '{"Args":["lock", "2", "344cFc3B8635f72F14200aAf2168d9f75df86FD3", "1000"]}' -C mychannel
```

- **getProxyHash**

获取某条链绑定的proxy：

```
docker exec cliMagnetoCorp peer chaincode invoke -n peth -c '{"Args":["getProxyHash", "2"]}' -C mychannel
```

- **getAssetHash**

获取某条链对应的资产hash：

```
docker exec cliMagnetoCorp peer chaincode invoke -n peth -c '{"Args":["getAssetHash", "2"]}' -C mychannel
```

- **getLockProxyAddr**

获得合约锁定资产的地址（如果开启跨链的话）：

```
docker exec cliMagnetoCorp peer chaincode invoke -n peth -c '{"Args":["getLockProxyAddr"]}' -C mychannel
```

- **isCrossChainOn**

是否开启跨链功能：

```
docker exec cliMagnetoCorp peer chaincode invoke -n peth -c '{"Args":["isCrossChainOn"]}' -C mychannel
```

**ERC20部分：**

- **name**

获取token名字：

```
docker exec cliMagnetoCorp peer chaincode invoke -n peth -c '{"Args":["name"]}' -C mychannel
```

- **symbol**

获取token符号：

```
docker exec cliMagnetoCorp peer chaincode invoke -n peth -c '{"Args":["symbol"]}' -C mychannel
```

- **decimal**

获取token精度：

```
docker exec cliMagnetoCorp peer chaincode invoke -n peth -c '{"Args":["decimal"]}' -C mychannel
```

- **totalSupply**

获取token总量：

```
docker exec cliMagnetoCorp peer chaincode invoke -n peth -c '{"Args":["totalSupply"]}' -C mychannel
```

- **getOwner**

获取token的管理员：

```
docker exec cliMagnetoCorp peer chaincode invoke -n peth -c '{"Args":["getOwner"]}' -C mychannel
```

- **balanceOf**

查询余额，结果需要通过big.Int解析：

```
docker exec cliMagnetoCorp peer chaincode invoke -n peth -c '{"Args":["balanceOf", "9b5826263c1e499cfc4c12db8ee98ac1f7584117"]}' -C mychannel
```

- **mint**

增发代币，仅可以由owner调用：

```
docker exec cliMagnetoCorp peer chaincode invoke -n peth -c '{"Args":["mint", "9b5826263c1e499cfc4c12db8ee98ac1f7584117", "10000"]}' -C mychannel
```

- **transfer**

转账：

```
docker exec cliMagnetoCorp peer chaincode invoke -n peth -c '{"Args":["transfer", "9b5826263c1e499cfc4c12db8ee98ac1f7584117", "10000"]}' -C mychannel
```

- **approve**

允许他人使用自己的代币，`9b5826263c1e499cfc4c12db8ee98ac1f7584117`允许`8b5826263c1e499cfc4c12db8ee98ac1f7584117`使用他10000的peth：

```
docker exec cliMagnetoCorp peer chaincode invoke -n peth -c '{"Args":["approve", "9b5826263c1e499cfc4c12db8ee98ac1f7584117", "8b5826263c1e499cfc4c12db8ee98ac1f7584117", "10000"]}' -C mychannel
```

- **transferFrom**

approve之后，`8b5826263c1e499cfc4c12db8ee98ac1f7584117`可以通过transferFrom使用peth：

```
docker exec cliMagnetoCorp peer chaincode invoke -n peth -c '{"Args":["transferFrom", "9b5826263c1e499cfc4c12db8ee98ac1f7584117", "8b5826263c1e499cfc4c12db8ee98ac1f7584117", "10000"]}' -C mychannel
```

- **allowance**

查询`9b5826263c1e499cfc4c12db8ee98ac1f7584117`给`8b5826263c1e499cfc4c12db8ee98ac1f7584117`允许的金额：

```
docker exec cliMagnetoCorp peer chaincode invoke -n peth -c '{"Args":["allowance", "9b5826263c1e499cfc4c12db8ee98ac1f7584117", "8b5826263c1e499cfc4c12db8ee98ac1f7584117"]}' -C mychannel
```

- **transferOwnership**

更换管理员，仅能由管理员调用：

```
docker exec cliMagnetoCorp peer chaincode invoke -n peth -c '{"Args":["transferOwnership", "9b5826263c1e499cfc4c12db8ee98ac1f7584117"]}' -C mychannel
```

- **increaseAllowance**

增加允许他人使用的金额：

```
docker exec cliMagnetoCorp peer chaincode invoke -n peth -c '{"Args":["increaseAllowance", "9b5826263c1e499cfc4c12db8ee98ac1f7584117", "8b5826263c1e499cfc4c12db8ee98ac1f7584117", "10000"]}' -C mychannel
```

- **decreaseAllowance**

减少允许他人使用的金额：

```
docker exec cliMagnetoCorp peer chaincode invoke -n peth -c '{"Args":["decreaseAllowance", "9b5826263c1e499cfc4c12db8ee98ac1f7584117", "8b5826263c1e499cfc4c12db8ee98ac1f7584117", "10000"]}' -C mychannel
```

- **burn**

销毁自己的代币：

```
docker exec cliMagnetoCorp peer chaincode invoke -n peth -c '{"Args":["burn", "10000"]}' -C mychannel
```

