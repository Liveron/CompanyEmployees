using System.Linq.Expressions;

namespace Repository;

public abstract class RepositoryBase<T>(RepositoryContext repositoryContext) : IRepositoryBase<T> where T : class
{
    protected RepositoryContext repositoryContext = repositoryContext;

    public IQueryable<T> FindAll(bool trackChanges)
    {
        if (!trackChanges)
            return repositoryContext.Set<T>().AsNoTracking();

        return repositoryContext.Set<T>();
    }

    public IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression, bool trackChanges)
    {
        if (!trackChanges)
            return repositoryContext.Set<T>().Where(expression).AsNoTracking();

        return repositoryContext.Set<T>().Where(expression);        
    }

    public void Create(T entity)
    {
        repositoryContext.Set<T>().Add(entity);
    }

    public void Update(T entity)
    {
        repositoryContext.Set<T>().Update(entity);
    }

    public void Delete(T entity)
    {
        repositoryContext.Set<T>().Remove(entity);
    }
}
