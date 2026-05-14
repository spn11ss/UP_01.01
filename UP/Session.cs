using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UP
{
    /// <summary>
    /// Статический класс для хранения информации о текущем сеансе пользователя
    /// </summary>
    /// <remarks>
    /// Класс хранит данные о пользователе после успешной авторизации.
    /// Все свойства доступны глобально из любого места приложения.
    /// 
    /// Жизненный цикл:
    /// - Заполняется при входе пользователя (AuthWindow.Login())
    /// - Очищается при выходе из приложения (при закрытии MainWindow)
    /// - Данные сохраняются только на время работы приложения
    /// </remarks>
    public static class Session
    {
        public static Users CurrentUser { get; set; }
        public static bool IsAuthenticated => CurrentUser != null;
        public static bool IsAdmin => CurrentUser?.RoleId == 3;
        public static bool IsAuthor => CurrentUser?.RoleId == 2;
        public static bool IsReader => CurrentUser?.RoleId == 1;
        public static bool IsFrozen => CurrentUser?.IsFrozen == true;
        public static string FreezeReason => CurrentUser?.FreezeReason;
    }
}
