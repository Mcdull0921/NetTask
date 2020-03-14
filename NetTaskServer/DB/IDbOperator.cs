﻿using System;
using System.Collections.Generic;
using System.Text;

namespace NetTaskServer.DB
{
    public interface IDbOperator : IDisposable
    {
        IDbOperator Open();
        void Insert(long key, string value);
        void Insert(string key, string value);
        void Update(long key, string value);
        void Update(string key, string value);
        void UpdateByName(string userName, string newUserName, string value);
        List<string> Select(int startIndex, int length);
        string Get(long key);
        string Get(string key);
        void Delete(int index);
        void DeleteHash(string key);
        long GetLength();
        void Close();
        bool Exist(string key);
        int GetCount();
    }
}
