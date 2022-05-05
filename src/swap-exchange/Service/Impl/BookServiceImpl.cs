using System;
using SwapExchange.Entity;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace SwapExchange.Service.Implemention
{   
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(IBookService))]
    public class BookServiceImpl: IBookService,ITransientDependency
    {   
        private readonly IRepository<Book, Guid> _bookRepository;
        
        public BookServiceImpl(IRepository<Book, Guid> _repository)
        {
            _bookRepository = _repository;
        }
                
        
        public Guid Save()
        {
            Book book = new Book();
            book.Name = "AAA";
            book.Price = "23.98";
            var insertAsync = _bookRepository.InsertAsync(book);
            var resultId = insertAsync.Result.Id;
            Console.WriteLine(resultId);
            return resultId;
        }

        public Book GetById(Guid id)
        {
            return _bookRepository.GetAsync(book => book.Id == id).Result;
        }

        public void Update(Book book)
        {
            _bookRepository.UpdateAsync(book);
        }
    }
}