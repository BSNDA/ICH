package MyHelloPoly

import (
	"fmt"
	"github.com/hyperledger/fabric/core/chaincode/shim"
	"github.com/hyperledger/fabric/protos/peer"
	"github.com/polynetwork/fabric-contract/utils"
	"time"
)

type HelloPoly struct {
}

func setLogger(logInfo ...interface{}) {
	t := time.Now()
	fmt.Printf("%s  ->  %s\n", t.Format("2006-01-02 15:04:05.000"), logInfo)
}

/**
 * @Author AC
 * @Date 2020-11-4 18:27
 * @Description  合约初始化
 * @Param
 * @Return
 **/
func (t *HelloPoly) Init(stub shim.ChaincodeStubInterface) peer.Response {
	setLogger("Start initializing init method......")
	defer setLogger("End initialization of init method......")
	return shim.Success(nil)
}

/**
 * @Author AC
 * @Date 2020-11-4 18:27
 * @Description  合约调用
 * @Param
 * @Return
 **/
func (t *HelloPoly) Invoke(stub shim.ChaincodeStubInterface) peer.Response {
	function, args := stub.GetFunctionAndParameters()
	switch function {
	case "say":
		return t.say(stub, args)
	case "hear":
		return t.hear(stub, args)
	case "getValue":
		return t.getValue(stub, args)
	case "getMyAddr":
		return t.getMyAddr(stub)
	default:
		setLogger("Unavailable method！")
		break
	}
	return shim.Error("Unavailable request！")
}

/**
 * @Author AC
 * @Date 2020-11-4 18:27
 * @Description 此方法用于对其它目标链进行跨链调用（此方法可自行定义）
 * @Param
 * @Return
 **/
func (t *HelloPoly) say(stub shim.ChaincodeStubInterface, args []string) peer.Response {
	setLogger("The say method is called......")
	defer setLogger("End called say method......")
	// 参数检查
	if len(args) != 4 {
		return shim.Error("Parameter error！！！")
	}
	// 保存数据
	if err:=stub.PutState(args[1],[]byte(args[2]));err!=nil{
		return shim.Error(fmt.Sprintf("Failed to save data: %v", err))
	}
	// 构建跨链管理合约调用参数
	invokeArgs := make([][]byte, 6)
	// 设置跨链管理合约被调用的方法
	invokeArgs[0] = []byte("crossChain")
	// 设置目标链在Poly网络中所对应的链ID（国内网所对应的链ID是8，国际网所对应的链是9）
	invokeArgs[1] = []byte(args[0])
	// 设置目标链应用合约地址，此参数不需要添加0x,如目标链应用合约地址为0x69d0ba0866ee3d9abd19b06ad8ac6f49023e19b8,则此参数传69d0ba0866ee3d9abd19b06ad8ac6f49023e19b8
	invokeArgs[2] = []byte(args[1])
	// 目标链应用合约方法
	invokeArgs[3] = []byte("hear")
	// 目标链应用合约所需要传递的跨链信息
	invokeArgs[4] = []byte(args[2])
	// 应用合约的名字
	invokeArgs[5] = []byte(args[3])
	// 调用跨链管理合约
	var resp = stub.InvokeChaincode(string("ccm"), invokeArgs, "")
	if resp.Status != shim.OK {
		return shim.Error(fmt.Sprintf("Failed to call the cross-chain management contract %s: %s", "ccm", resp.Message))
	}
	// 设置事件通知
	if err := stub.SetEvent("from_ccm", resp.Payload); err != nil {
		return shim.Error(fmt.Sprintf("Event setting failed: %v", err))
	}
	setLogger("Successfully call the cross-chain management contract for cross-chain: (target chain ID: %d, target chain contract address: %x, cross-chain message: %s)", args[1], args[1], args[2])
	return shim.Success(nil)
}

/**
 * @Author AC
 * @Date 2020-11-4 18:28
 * @Description 此方法用于对其它目标链进行跨链调用（此方法可自行定义）
 * @Param
 * @Return
 **/
func (t *HelloPoly) hear(stub shim.ChaincodeStubInterface, args []string) peer.Response {
	setLogger("The hear method is called......")
	defer setLogger("End called hear method......")
	// 参数检查
	if len(args) != 2 {
		return shim.Error("Parameter error！！！")
	}

	// 保存源链所提交的跨链信息
	if err:=stub.PutState(args[0],[]byte(args[1]));err!=nil{
		return shim.Error(fmt.Sprintf("Failed to save data: %v", err))
	}
	return shim.Success([]byte("SUCCESS"))
}

/**
 * @Author AC
 * @Date 2020-11-4 18:39
 * @Description  获取数据
 * @Param
 * @Return
 **/
func (t *HelloPoly) getValue(stub shim.ChaincodeStubInterface, args []string) peer.Response {
	setLogger("The getValue method is called......")
	defer setLogger("End getValue hear method......")
	// 参数检查
	if len(args) != 1 {
		return shim.Error("Parameter error！！！")
	}

	// 获取数据
	bytes,err:=stub.GetState(args[0])
	if err!=nil{
		return shim.Error(fmt.Sprintf("Get data error : %v", err))
	}
	return shim.Success(bytes)
}

/**
 * @Author AC
 * @Date 2020-11-4 18:40
 * @Description  获取应用合约地址信息
 * @Param
 * @Return
 **/
func (t *HelloPoly) getMyAddr(stub shim.ChaincodeStubInterface) peer.Response {
	setLogger("The getMyAddr method is called......")
	defer setLogger("End getMyAddr hear method......")
	// 获取应用合约地址信息(注：返回参数须通过hex.EncodeToString([]byte("\233X&&<\036I\234\374L\022\333\216\351\212\301\367XA\027"))方法进行解析)
	creator, err := utils.GetMsgSenderAddress(stub)
	if err != nil {
		return shim.Error(fmt.Sprintf("Failed to get sender: %v", err))
	}
	return shim.Success(creator.Bytes())
}
