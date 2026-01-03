using Byte.Core.Common.Cache;
using Byte.Core.Common.Extensions;
using Byte.Core.Common.Filters;
using Byte.Core.Common.Helpers;
using Byte.Core.Entity;
using Byte.Core.Models;
using Byte.Core.Repository;
using Byte.Core.SqlSugar;
using Byte.Core.Tools;
using Mapster;
using System.Linq.Expressions;
namespace Byte.Core.Business
{
    /// <summary>
    /// 角色-菜单
    /// </summary>
    public class MenuLogic : BaseBusinessLogic<long, Menu, MenuRepository>
    {
        private readonly IUnitOfWork _unitOfWork;
        public readonly Role_MenuRepository   _role_MenuRepository;
        public readonly DeptRepository _deptRepository;
        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="repository"></param>
        public MenuLogic(MenuRepository repository, IUnitOfWork unitOfWork, Role_MenuRepository  role_MenuRepository ,DeptRepository deptRepository) : base(repository)
        {
            _unitOfWork = unitOfWork;
            _role_MenuRepository = role_MenuRepository;
            _deptRepository = deptRepository;
        }




        /// <summary>
        /// 列表
        /// </summary>
        /// <returns></returns>
        public async Task<List<MenuTreeDTO>> GetTreeAsync()
        {

            var tree = await GetIQueryable().OrderBy(x => x.Order).Select<MenuTreeDTO>().ToTreeAsync(it => it.Children, it => it.ParentId, 0, it => it.Id);
            return tree;
        }

        /// <summary>
        /// 下拉框
        /// </summary>55
        /// <param name="parentId"></param>
        /// <returns></returns>
        public async Task<List<MenuSelectDTO>> GetTreeSelectAsync(long parentId = 0)
        {
            var tree = await GetIQueryable().OrderBy(x => x.Order).Select<MenuSelectDTO>().ToTreeAsync(it => it.Children, it => it.ParentId, parentId, it => it.Id);
            return tree;
        }



        /// <summary>
        /// 查询详情
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<MenuInfo> GetInfoAsync(long id)
        {
            var entity = await GetIQueryable(x => x.Id == id).Select(x => new MenuInfo
                {

                //Title = x.Title,
                //Path = null,
                //Component = null,
                //ComponentName = null,
                //ParentId = null,
                //Sort = 0,
                //Icon = null,
                //Type = (MenuTypeEnum)0,
                //KeepAlive = false,
                //Hidden = false,
                //Redirect = null,
                //AlwaysShow = false,
                //Status = false,
                //IsDeleted = false,
                //Roles = null,
                //Children = null

                },true).FirstAsync();

            entity.Buttons = await GetIQueryable(x => x.ParentId == id&&x.MenuType== MenuTypeEnum.按钮).Select(x => new MenuButton {
              Id = x.Id,
              Code = x.Path,
              Desc = x.Title,
              ParentId=x.ParentId,
              Status=x.Status
            }).ToListAsync();

            entity.Querys = await GetIQueryable(x => x.ParentId == id && x.MenuType == MenuTypeEnum.参数).Select(x => new MenuQuery
            {
                Id = x.Id,
                Key = x.Path,
                Value = x.PathParam,
                ParentId = x.ParentId,
                Status = x.Status
            }).ToListAsync();


            return entity;
        }

        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<long> AddAsync(UpdateMenuParam param)
        {
            Menu model = param.Adapt<Menu>();
            try
            {
                _unitOfWork.BeginTran();
         
                await AddAsync(model);

                var addButtons = param.Buttons?.Select(x => new Menu
                {
                    MenuType = MenuTypeEnum.按钮,
                    Title = x.Desc,
                    Path = x.Code.ToLower(),
                    ParentId = model.Id,
                    I18nKey=x.Desc,
                    Status = x.Status
                }).ToList();
                await AddRangeAsync(addButtons);



                var addQuerys = param.Querys?.Select(x => new Menu
                {
                    MenuType = MenuTypeEnum.参数,
                    PathParam = x.Value,
                    Path = x.Key,
                    ParentId = model.Id,
                    Status = x.Status
                }).ToList();
                await AddRangeAsync(addQuerys);


                _unitOfWork.CommitTran();
         
            }
            catch (Exception ex) { 
                _unitOfWork.RollbackTran();
                throw ex;
            }
            finally
            {
                //清除缓存
                MemoryCacheManager.RemoveCacheRegex(AppConfig.RoleCaChe);
            }
            return model.Id;
        }

        /// <summary>
        /// 编辑
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<long> UpdateAsync(UpdateMenuParam param)
        {
       
            try
            {
                _unitOfWork.BeginTran();
                await UpdateAsync(x => x.Id == param.Id, x => new Menu
                {
                    Name = param.Name,
                    Component = param.Component,
                    Path = param.Path,
                    Icon = param.Icon,
                    LocalIcon = param.LocalIcon,
                    IconFontSize = param.IconFontSize,
                    Order = param.Order,
                    Href = param.Href,
                    HideInMenu = param.HideInMenu,
                    ActiveMenu = param.ActiveMenu,
                    MultiTab = param.MultiTab,
                    FixedIndexInTab = param.FixedIndexInTab,
                    PathParam = param.PathParam,
                    ParentId = param.ParentId,
                    MenuType = param.MenuType,
                    KeepAlive = param.KeepAlive,
                    Constant = param.Constant,
                    Title = param.Title,
                    I18nKey = param.I18nKey,
                    Redirect = param.Redirect,
                    Status = param.Status,
                });
                //删除
                await DeleteAsync(x=>x.MenuType== MenuTypeEnum.按钮&& x.ParentId==param.Id && (param.Buttons .Count==0||param.Buttons.Any(y=>y.Id!= x.Id )));
                //新增/编辑
                var buttons = param.Buttons.Select(x => new Menu
                {
                    Id = x.Id,
                    MenuType = MenuTypeEnum.按钮,
                    Title = x.Desc,
                    I18nKey = x.Desc,
                    Path = x.Code.ToLower(),
                    ParentId = param.Id,
                    Status = x.Status
                }).ToList();
                await   _unitOfWork.GetDbClient().Storageable(buttons).ExecuteCommandAsync();

                //删除
                await DeleteAsync(x => x.MenuType == MenuTypeEnum.参数 && x.ParentId == param.Id && (param.Querys.Count == 0 || param.Querys.Any(y => y.Id != x.Id)));
                //新增/编辑
                var querys = param.Querys.Select(x => new Menu
                {
                    Id = x.Id,
                    MenuType = MenuTypeEnum.参数,
                    PathParam = x.Value,
                    Path = x.Key,
                    ParentId = param.Id,
                    Status = x.Status
                }).ToList();
                await _unitOfWork.GetDbClient().Storageable(querys).ExecuteCommandAsync();




                _unitOfWork.CommitTran();
            }
            catch (Exception ex)
            {
                _unitOfWork.RollbackTran();
                throw ex;
            }
            finally {
                //清除缓存
                MemoryCacheManager.RemoveCacheRegex(AppConfig.RoleCaChe);
            }
            
            return param.Id;
           
        }


        /// <summary>
        /// 设置状态
        /// </summary>
        /// <param name="id"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public async Task<int> SetStatusAsync(int id, bool status) => await UpdateAsync(x => id == x.Id, x => new Menu { Status = status });




        /// <summary>
        /// 菜单下拉
        /// </summary>
        /// <returns></returns>
        public async Task<List<RouteSelectDTO>> SelectAsync()
        {

            var db = await GetIQueryable(x => x.Status&& !x.Constant &&x.MenuType != MenuTypeEnum.参数).OrderBy(x => x.Order).Select(x => new RouteSelectDTO
            {
                Id = x.Id,
                ParentId = x.ParentId,
                Title = x.Title,
                MenuType=x.MenuType,
            }).ToListAsync();
            var list = new List<RouteSelectDTO>();
            //递归
            db.ForEach(x =>
            {
                x.Children = db.Where(y => y.ParentId == x.Id)?.ToList();
                x.Children= x.Children.Count()==0?null:x.Children;
                if (x.ParentId == 0)
                {
                    list.Add(x);
                }
            });

            return list;
        }

        /// <summary>
        /// 通过角色Id获取菜单数组
        /// </summary>
        /// <param name="roleId"></param>
        /// <returns></returns>
        public async Task<long[]> GetByRoleIdAsync(long roleId)
        {
            var ids = await  _role_MenuRepository.GetIQueryable(x => x.RoleId == roleId).Select(x => x.MenuId).ToArrayAsync();
            return ids;
        }

        /// <summary>
        /// 通过角色Id添加菜单数组
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task SetByRoleIdAsync(SetByRoleIdDTO param)
        {
            try
            {
                _unitOfWork.BeginTran();


                await _role_MenuRepository.DeleteAsync(x => x.RoleId == param.RoleId);
                var list = param.MenuIds.Select(x => new Role_Menu
                {
                    RoleId = param.RoleId,
                    MenuId = x
                }).ToList();
                await _role_MenuRepository.AddRangeAsync(list);
                //清除缓存
                MemoryCacheManager.RemoveCacheRegex(AppConfig.RoleCaChe);
                _unitOfWork.CommitTran();
            }
            catch
            {

                _unitOfWork.RollbackTran();
                throw new BusException("更新失败", 0);
            }
        }


        /// <summary>
        /// 判断路由是否存在
        /// </summary>
        /// <returns></returns>
        public async  Task<bool> IsRouteExistAsync(string name) {


            var codes = CurrentUser.RoleCodes;
            Expression<Func<Menu, bool>> where = x => x.Status && x.MenuType != MenuTypeEnum.按钮 && x.MenuType != MenuTypeEnum.参数;
            if (!codes.Contains(AppConfig.Root))
            {
                where = where.And(x => x.Roles.Any(y => codes.Contains(y.Code)));
            }
            where = where.Or(x => x.Name == name);
            return await GetIQueryable(where).AnyAsync();
        }



        /// <summary>
        /// 获取当前用户的路由
        /// </summary>
        /// <returns></returns>
        public async Task<MyRouteDTO> GetRoutesAsync()
        {
            var codes = CurrentUser.RoleCodes;
            var home = await _deptRepository.GetIQueryable(x => x.Id == CurrentUser.DeptId).Select(x => x.Home).FirstAsync();
            Expression<Func<Menu, bool>> where = x => !x.Constant;
            if (!codes.Contains(AppConfig.Root))
            {
                where = where.And(x => x.Roles.Any(y => codes.Contains(y.Code)));
            }
            where= where.Or(x => x.Name == home);
             var model = new MyRouteDTO()
            {
                Home=  home,
                Routes = await GetRoutesAsync(where)

            };
            return model;
        }

        /// <summary>
        /// 获取常量路由,公共路由
        /// </summary>
        /// <returns></returns>5
        public async Task<List<RouteDTO>> GetConstantRoutesAsync() {

            return await GetRoutesAsync(x => x.Constant);
        }
        #region 私有获取路由
        private async Task<List<RouteDTO>> GetRoutesAsync(Expression<Func<Menu, bool>> where) {


          
            where = where.And(x => x.Status && x.MenuType != MenuTypeEnum.按钮 && x.MenuType != MenuTypeEnum.参数);
           
            var db = await GetIQueryable(where).OrderBy(x => x.Order).ToListAsync();
            //查出请求参数有
            var querys = GetIQueryable(x => x.MenuType == MenuTypeEnum.参数&& db.Any(y=>y.Id==x.ParentId)).Select(x => new MenuQuery { 
                ParentId=   x.ParentId,
                Key=  x.Path,
                Status = x.Status,
                Value = x.PathParam
            }).ToList();

            var entity = db.Select(x => new RouteDTO()
            {
                Id = x.Id,
                Meta = new RouteMeta
                {
                    Icon = x.Icon,
                    LocalIcon = x.LocalIcon,
                    IconFontSize = x.IconFontSize,
                    Order = x.Order,
                    Href = x.Href,
                    HideInMenu = x.HideInMenu,
                    ActiveMenu = x.ActiveMenu,
                    MultiTab = x.MultiTab,
                    FixedIndexInTab = x.FixedIndexInTab,
                    //Query = x.Query,
                    KeepAlive = x.KeepAlive,
                    Constant = x.Constant,
                    Title = x.Title,
                    I18nKey = x.I18nKey,
                    Query = querys.Where(y => y.ParentId == x.Id && !string.IsNullOrEmpty(y.Value) && !string.IsNullOrEmpty(y.Key)).ToList()
                },
                Props = x.Props,
                Name = x.Name,
                Component = Component(x.Layout, x.Component),
                Path = x.Path + x.PathParam,
                ParentId = x.ParentId,
                //Type = x.Type,
                Redirect = x.Redirect,
                //Status = x.Status,

            }).ToList();


            List<RouteDTO> list = new List<RouteDTO>();

            entity.ForEach(x =>
            {
                     
               
                //x.Params = new Dictionary<string, dynamic>() { { "TableName", "User" } };
                x.Children = entity.Where(y => y.ParentId == x.Id).Select(y =>
                {
                    y.Path = x.Path + y.Path;
                    return y;
                }
                    ).ToList();
                if (x.ParentId == 0)
                {
                    list.Add(x);
                }
            });
            return list;

            //返回组件
            string Component(LayoutTypeEnum? layout ,string component) {
                var str = string.Empty;
                if (layout != null && !string.IsNullOrEmpty(component)) {
                    Console.WriteLine($"{layout}&{component}");
                    str = EnumHelper.GetEnumDescription(layout) + "$" + component;
                }
            
                else
                    str = layout != null ? EnumHelper.GetEnumDescription(layout) : component;
                return str;
            }
        }
        #endregion
    }
}