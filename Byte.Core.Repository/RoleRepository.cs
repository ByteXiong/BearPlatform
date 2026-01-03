using Byte.Core.SqlSugar;
using Byte.Core.Entity;
using Byte.Core.Common.Filters;
using System.Linq.Expressions;
using Byte.Core.Common.Extensions;
using SqlSugar;
using Byte.Core.Tools;

namespace Byte.Core.Repository
{
    /// <summary>
    /// 角色
    /// </summary>
    public class RoleRepository : BaseRepository<long, Role>
    {
        public RoleRepository(IUnitOfWork unitOfWork) : base(unitOfWork)
        {
        }


        public override ISugarQueryable<Role> GetIQueryable(Expression<Func<Role, bool>> where = null, bool needAuth = true)
        {
            if (needAuth)
            {
                where = where.And(x => CurrentUser.RoleCodes.Contains(x.Code));
            }

            return base.GetIQueryable(where, needAuth);
        }


        public override async Task<int> AddAsync(Role entity)
        {
            await OnlyAccountAsync(entity.Code);
            return await base.AddAsync(entity);
        }
        public override async Task<int> UpdateAsync(Role entity, Expression<Func<Role, object>> lstIgnoreColumns = null, bool isLock = true)
        {
            await OnlyAccountAsync(entity.Code, entity.Id);
            return await base.UpdateAsync(entity, lstIgnoreColumns, isLock);
        }

        /// <summary>
        /// 账号唯一
        /// </summary>
        /// <returns></returns>
        private async Task OnlyAccountAsync(string code, long? id = default)
        {

            Expression<Func<Role, bool>> where = x => x.Code == code;
            if (id != default)
            {
                where = where.And(x => x.Id != id);
            }
            var any = await GetIQueryable(where).AnyAsync();
            if (any) throw new BusException("角色编码已存在");
        }
    }
}