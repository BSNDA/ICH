/*
 * Copyright (C) 2020 The poly network Authors
 * This file is part of The poly network library.
 *
 * The  poly network  is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * The  poly network  is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 * You should have received a copy of the GNU Lesser General Public License
 * along with The poly network .  If not, see <http://www.gnu.org/licenses/>.
 */
package utils

import (
	"fmt"
	"github.com/golang/protobuf/ptypes/timestamp"
	"github.com/hyperledger/fabric/core/chaincode/shim"
	pb "github.com/hyperledger/fabric/protos/peer"
)

type CCStubMock struct {
	Mem  map[string][]byte
	Args [][]byte
	CA string
}

func (mock *CCStubMock) GetArgs() [][]byte {
	return mock.Args
}

func (mock *CCStubMock) GetStringArgs() []string {
	args := mock.GetArgs()
	strargs := make([]string, 0, len(args))
	for _, barg := range args {
		strargs = append(strargs, string(barg))
	}
	return strargs
}

func (mock *CCStubMock) GetFunctionAndParameters() (string, []string) {
	return "", nil
}

func (mock *CCStubMock) GetArgsSlice() ([]byte, error) {
	return nil, nil
}

func (mock *CCStubMock) GetTxID() string {
	return ""
}

func (mock *CCStubMock) GetChannelID() string {
	return "1"
}

func (mock *CCStubMock) InvokeChaincode(chaincodeName string, args [][]byte, channel string) pb.Response {
	for _, v := range args {
		fmt.Println(string(v))
	}
	return shim.Success(nil)
}

func (mock *CCStubMock) GetState(key string) ([]byte, error) {
	return mock.Mem[key], nil
}

func (mock *CCStubMock) PutState(key string, value []byte) error {
	mock.Mem[key] = value
	return nil
}

func (mock *CCStubMock) DelState(key string) error {
	delete(mock.Mem, key)
	return nil
}

func (mock *CCStubMock) SetStateValidationParameter(key string, ep []byte) error {
	return nil
}

func (mock *CCStubMock) GetStateValidationParameter(key string) ([]byte, error) {
	return nil, nil
}

func (mock *CCStubMock) GetStateByRange(startKey, endKey string) (shim.StateQueryIteratorInterface, error) {
	return nil, nil
}

func (mock *CCStubMock) GetStateByRangeWithPagination(startKey, endKey string, pageSize int32, bookmark string) (shim.StateQueryIteratorInterface, *pb.QueryResponseMetadata, error) {
	return nil, &pb.QueryResponseMetadata{}, nil
}

func (mock *CCStubMock) GetStateByPartialCompositeKey(objectType string, keys []string) (shim.StateQueryIteratorInterface, error) {
	return nil, nil
}

func (mock *CCStubMock) GetStateByPartialCompositeKeyWithPagination(objectType string, keys []string,
	pageSize int32, bookmark string) (shim.StateQueryIteratorInterface, *pb.QueryResponseMetadata, error) {
	return nil, &pb.QueryResponseMetadata{}, nil
}

func (mock *CCStubMock) CreateCompositeKey(objectType string, attributes []string) (string, error) {
	return "", nil
}

func (mock *CCStubMock) SplitCompositeKey(compositeKey string) (string, []string, error) {
	return "", nil, nil
}

func (mock *CCStubMock) GetQueryResult(query string) (shim.StateQueryIteratorInterface, error) {
	return nil, nil
}

func (mock *CCStubMock) GetQueryResultWithPagination(query string, pageSize int32, bookmark string) (shim.StateQueryIteratorInterface, *pb.QueryResponseMetadata, error) {
	return nil, &pb.QueryResponseMetadata{}, nil
}

func (mock *CCStubMock) GetHistoryForKey(key string) (shim.HistoryQueryIteratorInterface, error) {
	return nil, nil
}

func (mock *CCStubMock) GetPrivateData(collection, key string) ([]byte, error) {
	return nil, nil
}

func (mock *CCStubMock) GetPrivateDataHash(collection, key string) ([]byte, error) {
	return nil, nil
}

func (mock *CCStubMock) PutPrivateData(collection string, key string, value []byte) error {
	return nil
}

func (mock *CCStubMock) DelPrivateData(collection, key string) error {
	return nil
}

func (mock *CCStubMock) SetPrivateDataValidationParameter(collection, key string, ep []byte) error {
	return nil
}

func (mock *CCStubMock) GetPrivateDataValidationParameter(collection, key string) ([]byte, error) {
	return nil, nil
}

func (mock *CCStubMock) GetPrivateDataByRange(collection, startKey, endKey string) (shim.StateQueryIteratorInterface, error) {
	return nil, nil
}

func (mock *CCStubMock) GetPrivateDataByPartialCompositeKey(collection, objectType string, keys []string) (shim.StateQueryIteratorInterface, error) {
	return nil, nil
}

func (mock *CCStubMock) GetPrivateDataQueryResult(collection, query string) (shim.StateQueryIteratorInterface, error) {
	return nil, nil
}

func (mock *CCStubMock) GetCreator() ([]byte, error) {
	return []byte(mock.CA), nil
}

func (mock *CCStubMock) GetTransient() (map[string][]byte, error) {
	return nil, nil
}

func (mock *CCStubMock) GetBinding() ([]byte, error) {
	return nil, nil
}

func (mock *CCStubMock) GetDecorations() map[string][]byte {
	return nil
}

func (mock *CCStubMock) GetSignedProposal() (*pb.SignedProposal, error) {
	return &pb.SignedProposal{}, nil
}

func (mock *CCStubMock) GetTxTimestamp() (*timestamp.Timestamp, error) {
	return &timestamp.Timestamp{}, nil
}

func (mock *CCStubMock) SetEvent(name string, payload []byte) error {
	return nil
}

func (mock *CCStubMock) SetNewCA(ca string) {
	mock.CA = ca
}