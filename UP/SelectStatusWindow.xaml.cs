using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace UP
{
    /// <summary>
    /// Логика взаимодействия для SelectStatusWindow.xaml
    /// </summary>
    /// <remarks>
    /// Окно выбора статуса для добавления книги в пользовательский список чтения.
    /// Предоставляет четыре варианта статуса:
    /// - В планах (BookStatusId = 1)
    /// - Читаю (BookStatusId = 2)
    /// - Прочитано (BookStatusId = 3)
    /// - Заброшено (BookStatusId = 4)
    /// 
    /// После выбора статуса создаётся запись в таблице ReadingLists,
    /// связывающая пользователя, книгу и выбранный статус.
    /// </remarks>
    public partial class SelectStatusWindow : Window
    {
        /// <summary>
        /// ID книги, которую пользователь хочет добавить в список
        /// </summary>
        private int bookId;

        /// <summary>
        /// Конструктор окна выбора статуса
        /// </summary>
        /// <param name="bookId">ID книги для добавления в список</param>
        /// <remarks>
        /// Выполняет:
        /// 1. InitializeComponent() - загружает XAML разметку окна
        /// 2. Сохраняет переданный bookId в поле класса для использования при создании записи
        /// 
        /// Принимает ID книги из CatalogPage или BookWindow при нажатии кнопки "В список".
        /// </remarks>
        public SelectStatusWindow(int bookId)
        {
            InitializeComponent();
            this.bookId = bookId;
        }

        /// <summary>
        /// Обработчик нажатия на кнопку выбора статуса
        /// </summary>
        /// <param name="sender">Кнопка, содержащая выбранный статус (PlanBtn, ReadingBtn, ReadBtn или AbandonedBtn)</param>
        /// <param name="e">Параметры события нажатия кнопки</param>
        /// <remarks>
        /// Алгоритм:
        /// 1. Извлекает ID статуса из свойства Tag кнопки (1-4)
        /// 2. Создаёт новый объект ReadingLists с данными:
        ///    - UserId = ID текущего авторизованного пользователя (Session.CurrentUser.ID)
        ///    - BookId = ID книги, переданный в конструктор
        ///    - BookStatusId = выбранный ID статуса
        ///    - AddedDate = текущая дата и время
        /// 3. Сохраняет запись в базу данных
        /// 4. Показывает сообщение об успешном добавлении
        /// 5. Закрывает окно выбора статуса
        /// 
        /// Использует Session.CurrentUser для получения ID текущего пользователя.
        /// </remarks>
        private void StatusBtn_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            int statusId = int.Parse(btn.Tag.ToString());

            var readingList = new ReadingLists
            {
                UserId = Session.CurrentUser.ID,
                BookId = bookId,
                BookStatusId = statusId,
                AddedDate = DateTime.Now
            };

            Core.Context.ReadingLists.Add(readingList);
            Core.Context.SaveChanges();

            MessageBox.Show("Книга добавлена в список!", "Успех",
                MessageBoxButton.OK, MessageBoxImage.Information);

            this.Close();
        }
    }
}
