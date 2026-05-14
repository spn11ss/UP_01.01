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
    /// Логика взаимодействия для MoveStatusWindow.xaml
    /// </summary>
    /// <remarks>
    /// Окно для перемещения книги между списками чтения.
    /// Позволяет пользователю изменить статус книги (В планах/Читаю/Прочитано/Заброшено).
    /// 
    /// Отличия от SelectStatusWindow:
    /// - Используется для ИЗМЕНЕНИЯ существующего статуса (UPDATE), а не для добавления новой записи
    /// - Принимает additional параметры: readingListId и currentStatusId
    /// - Скрывает кнопку с текущим статусом, чтобы нельзя было выбрать тот же самый статус
    /// 
    /// Вызывается из ReadingListsPage при нажатии кнопки "Переместить".
    /// </remarks>
    public partial class MoveStatusWindow : Window
    {
        /// <summary>
        /// ID записи в таблице ReadingLists (связь пользователь-книга)
        /// </summary>
        private int readingListId;

        /// <summary>
        /// Конструктор окна перемещения книги
        /// </summary>
        /// <param name="bookId">ID книги (используется для передачи, но в данном окне не требуется)</param>
        /// <param name="readingListId">ID записи в ReadingLists, которую нужно обновить</param>
        /// <param name="currentStatusId">Текущий статус книги (1-4), чтобы скрыть соответствующую кнопку</param>
        /// <remarks>
        /// Выполняет:
        /// 1. InitializeComponent() - загрузка XAML разметки окна
        /// 2. Сохраняет readingListId для использования при обновлении статуса
        /// 3. Скрывает кнопку с текущим статусом (чтобы нельзя было выбрать тот же статус)
        /// 
        /// Параметр bookId принимается для единообразия вызова (чтобы сигнатура совпадала с SelectStatusWindow),
        /// но в данном окне не используется, так как обновление идёт через readingListId.
        /// 
        /// Пример: если книга имеет статус "Читаю" (currentStatusId = 2),
        /// то кнопка "Читаю" (ReadingBtn) будет скрыта.
        /// </remarks>
        public MoveStatusWindow(int bookId, int readingListId, int currentStatusId)
        {
            InitializeComponent();
            this.readingListId = readingListId;

            if (currentStatusId == 1) PlanBtn.Visibility = Visibility.Collapsed;  
            if (currentStatusId == 2) ReadingBtn.Visibility = Visibility.Collapsed;
            if (currentStatusId == 3) ReadBtn.Visibility = Visibility.Collapsed;
            if (currentStatusId == 4) AbandonedBtn.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Обработчик нажатия на кнопку выбора нового статуса
        /// </summary>
        /// <param name="sender">Кнопка выбранного статуса (PlanBtn, ReadingBtn, ReadBtn или AbandonedBtn)</param>
        /// <param name="e">Параметры события нажатия кнопки</param>
        /// <remarks>
        /// Алгоритм:
        /// 1. Извлекает ID нового статуса из свойства Tag кнопки (1-4)
        /// 2. Находит запись в таблице ReadingLists по сохранённому readingListId
        /// 3. Обновляет поле BookStatusId на новый статус
        /// 4. Сохраняет изменения в базе данных (Core.Context.SaveChanges())
        /// 5. Показывает сообщение об успешном перемещении с указанием названия статуса
        /// 6. Закрывает окно
        /// 
        /// В отличие от SelectStatusWindow, здесь не создаётся новая запись,
        /// а обновляется существующая (UPDATE вместо INSERT).
        /// </remarks>
        private void StatusBtn_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            int newStatusId = int.Parse(btn.Tag.ToString());

            var entry = Core.Context.ReadingLists.FirstOrDefault(rl => rl.ID == readingListId);
            if (entry != null)
            {

                entry.BookStatusId = newStatusId;
                Core.Context.SaveChanges();

                string statusName = "";
                switch (newStatusId)
                {
                    case 1: statusName = "В планах"; break;
                    case 2: statusName = "Читаю"; break;
                    case 3: statusName = "Прочитано"; break;
                    case 4: statusName = "Заброшено"; break;
                }

                MessageBox.Show($"Книга перемещена в список '{statusName}'!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }

            this.Close();
        }
    }
}