using System;
using SwapExchange.Entity;

namespace SwapExchange.Service
{
    public interface IBookService
    {
        public Guid Save();
        public Book GetById(Guid id);
        public void Update(Book book);
    }
}