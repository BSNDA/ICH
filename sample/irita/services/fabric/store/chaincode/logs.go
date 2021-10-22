package chaincode

import (
	"fmt"
	"time"
)

func GetTime() string {

	return time.Now().Format("2006-01-02 15:04:05.000")
}

func LogInfo(f string, a ...interface{}) {
	fmt.Println(fmt.Sprintf("%s [%s] => %s", GetTime(), "INFO", fmt.Sprintf(f, a...)))
}
