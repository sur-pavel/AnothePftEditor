using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using FarsiLibrary.Win;
using FastColoredTextBoxNS;
using ManagedIrbis;
using ManagedIrbis.ImportExport;
using ManagedIrbis.Pft;
using ManagedIrbis.Pft.Infrastructure;
using PftEditor.Properties;

namespace PftEditor
{
    public partial class PowerfulCSharpEditor : Form
    {
        private MarcRecord _record;
        private PftTokenList _tokenList;
        private PftProgram _program;

        string[] keywords =
        {
            "fi,", "and", "or", "then", "else", "if"
        };

        string[] methods = {"uf()", "unifor()"};

        string[] snippets =
        {
            "if ^ then\n \nfi,\n",
            "if ^ then\n \nelse\n \nfi,\n",
            "uf('^')", "unifor('^')",
        };

        public static Dictionary<string, string> _unifors = new Dictionary<string, string>()
        {
            {"uf('+95^')", "Вернуть длину исходной строки"},
            {
                "uf('+96^#')", "Вернуть часть строки\n" +
                               "+96A*SSS.NNN#<строка>\n" +
                               "A – направление: 0 – с начала строки; 1 – с конца;\n" +
                               "SSS – смещение\n" +
                               "NNN – кол-во символов\n" +
                               "&uf('+960*0.4#'v100)"
            },
            {
                "uf('Q'^)", "Вернуть заданную строку в нижнем регистре\n" +
                            "&unifor('Q'v200)"
            },
            {"uf('+97'^)", "Вернуть заданную строку в верхнем регистре"},
            {
                "uf('+98^',)", "Заменить в заданной строке один символ на другой (регистр учитывается)\n" +
                               "+98ab<строка>\n" +
                               "a – заменяемый символ\n" +
                               "b – заменяющий символ\n" +
                               "&uf('+98 0',f(1,5,0))"
            },
            {
                "uf('+9F^')", "Вернуть ANSI-символ с заданным кодом\n" +
                              "+9F<код>"
            },
            {
                "uf('+9I^')",
                "Заменить в данных одни символы другими\n" +
                "+9I!AAAA!/BBBB/<данные>"
            },
            {
                "uf('+9S^')", "Возвращает позицию первого символа найденного вхождения подстроки в исходную строку\n" +
                              "+9S!подстрока!<исходная_строка>"
            },
            {
                "uf('E^')", " Вернуть заданное количество слов с начала строки\n" +
                            "EN<строка>\n" +
                            " N – количество слов (одна цифра)\n" +
                            "&unifor('E3'v200^a)"
            },
            {
                "uf('F'^)", "Вернуть часть строки, начиная со следующего слова после указанного и до конца строки\n" +
                            "FN<строка>\n" +
                            " N – количество слов (одна цифра)\n" +
                            "&unifor('F3'v200^a)"
            },
            {"uf('+9G^')", "Преобразовать заданную строку в список слов"},
            {
                "uf('+3E',^)", "Кодирование данных для представления в URL\n" +
                               "+3E<данные>\n" +
                               "&unifor('+3E',v1007)"
            },
            {"uf('+3D^')", "Декодирование данных из URL"},
            {"uf('+3+^')", "Перевод знака + в %2B"},
            {"uf('+3U^')", "Кодирование строки в UTF-8"},
            {"uf('+3W^')", "Декодирование строки из UTF-8"},
            {
                "uf('T^')", "Транслитерация кириллических символов с помощью латиницы\n" +
                            "TN<строка>\n" +
                            "N – вид таблицы транслитерации (0 или 1)\n" +
                            "&unifor('T0'V200)"
            },
            {
                "uf('G^')", "Вернуть часть строки до или начиная с заданного символа\n" +
                            "GNA<строка>\n" +
                            "N может принимать значения:\n" +
                            "0 (или A) – до заданного символа не включая его;\n" +
                            "1 (или B) – начиная с заданного символа;\n" +
                            "2 (или C) – после заданного символа;\n" +
                            "3 (или D) – после последнего вхождения заданного символа;\n" +
                            "4 (или E) – до последнего вхождения заданного символа (включая его);\n" +
                            "5 – до последнего вхождения заданного символа (не включая его)\n" +
                            "А – заданный символ. Символ обозначает самого себя, кроме\n" +
                            "# (обозначает любую цифру) и\n" +
                            "$ (обозначает любую букву)\n" +
                            "&unifor('G0#'v700)"
            },
            {
                "uf('9^')", "Удалить двойные кавычки из заданной строки\n" +
                            "&unifor('9'v200^a)"
            },
            {
                "uf('X^')", "Удаление из заданной строки фрагментов, выделенных угловыми скобками <>\n" +
                            "&unifor('X'v200)"
            },
            {
                "uf('+S^')",
                "Выводит нужную часть текста (до знака =, или после него) в заголовках <NNN=KKK>\n" +
                "+SN[строка]\n" +
                "N может принимать значения 0 для вывода текста после знака =, и 1 для вывода текста перед знаком =\n" +
                "&uf('+s0'v200^a)"
            },
            {
                "uf('='!^!)",
                "Сравнить заданное значение с маской\n" +
                "Возвращаемое значение: 1 – в случае положительного результата сравнения; 0 – в случае отрицательного\n" +
                "!<маска>!<значение>\n" +
                "Маска может содержать принятые символы маскирования * и ?" +
                "В общем случае маска может содержать несколько масок, отделенных друг от друга символом вертикальной черты |\n" +
                "&uf('='!'А-9'!v686)"
            },
            {
                @"uf('+\\^')", "Преобразование строки, удваивающее обратный слэш, или обратное\n" +
                               @"+\\N<строка>\n" +
                               "N может принимать значения\n" +
                               "0 - удвоение знаков обратного слэш;\n" +
                               "1 - преобразование удвоенных знаков слэш в одинарные\n" +
                               @"&uf('+\\0c:\\example.txt')"
            },
            {
                "uf('+R'^)", "Усечение кода рубрики в рубрикаторе ГРНТИ до вышестоящего\n" +
                             "&unifor('+R'v3)"
            },

            {"uf('+9R^')", "Преобразование римского числа в арабское"},
            {"uf('+9X^')", "Преобразование арабского числа в римское"},
            {"uf('3^')", "Выдача данных, связанных с датой и временем"},
            {"uf('+7^')", "Очистить (опустошить) все глобальные переменные"},
            {"uf('+7R^')", "Чтение глобальной переменной"},
            {"uf('+7W^')", "Запись глобальной переменной"},
            {"uf('+7U^')", "Добавление повторений глобальной переменной"},
            {"uf('+7M^')", "Логическое перемножение повторений двух переменных"},
            {"uf('+7S^')", "Логическое вычитание повторений двух переменных"},
            {"uf('+7A^')", "Логическое сложение повторений двух переменных"},
            {"uf('+7G^')", "Исключение неоригинальных повторений переменной"},
            {"uf('+7T^')", "Сортировка повторений переменной"},
            {"uf('+99^')", "Групповая установка глобальных переменных"},
            {"uf('0^')", " Выдать содержимое документа полностью в формате RTF"},
            {"uf('+0^')", "Выдать содержимое документа полностью во внутреннем представлении"},
            {"uf('++0^')", "Выдать содержимое документа полностью – только содержимое полей"},
            {"uf('+@^')", "Выдать содержимое документа полностью в формате JSON"},
            {"uf('+N^')", "Выдать количество повторений поля"},
            {"uf('A^')", "Выдать заданное повторение поля"},
            {"uf('P^')", "Выдать заданное оригинальное повторение поля"},
            {"uf('+E^')", "Возвращает порядковый номер заданного поля в записи"},
            {"uf('K^')", "Раскодировка через справочник"},
            {"uf('I^')", "Вернуть параметр из INI-файла"},
            {"uf('1^')", "Вернуть заданный подэлемент"},
            {"uf('J^')", "Вернуть кол-во ссылок для заданного термина"},
            {"uf('D^')", "Форматирование документа из другой БД"},
            {"uf('7^')", "Расформатирование группы связанных документов из другой БД"},
            {"uf('!^')", "Команда постредактуры: очистить результат расформатирования от двойных разделителей"},
            {"uf('+F^')", "Команда постредактуры: очистить результат расформатирования от RTF-конструкций"},
            {"uf('+1O^')", "Групповая мультираскодировка списка"},
            {"uf('+1I^')", "Исключение неоригинальных значений из списка"},
            {"uf('+1V^')", "Сортировка списка"},
            {"uf('+I^')", "Формирования ссылки (гиперссылки)"},
            {"uf('+6^')", "Выдать статус записи"},
            {"uf('+D^')", "Выдать имя базы данных"},
            {"uf('2^')", "Вернуть последний номер MFN в базе +1"},
            {"uf('+9V^')", "Вернуть номер поколения ИРБИС"},
            {"uf('4N^')", "Форматировать предыдущую копию текущей записи"},
            {"uf('+4^')", "Выдача метки, порядкового номера и значения поля в соответствии с номером повторения"},
            {"uf('+5^')", "Выдача элемента списка/справочника в соответствии с номером повторения"},
            {"uf('+90^')", "Вернуть номер текущего повторения в повторяющейся группе"},
            {"uf('B^')", "Выдать библиографическую свертку документа"},
            {"uf('C^')", "Контроль ISSN/ISBN"},
            {"uf('L^')", "Вернуть окончание термина"},
            {"uf('U^')", "Кумуляция номеров журналов"},
            {"uf('V^')", "Декумуляция номеров журналов"},
            {"uf('W^')", "Контроль кумуляции"},
            {"uf('O^')", "Вывод сведений о всех экземплярах по всем местам хранения"},
            {"uf('Y^')", "Возвращает данные о всех свободных (не выданных) экземплярах по всем местах хранения"},
            {"uf('+91^')", "Вернуть имя файла из заданного полного пути/имени"},
            {"uf('+92^')", "Вернуть путь из заданного полного пути/имени"},
            {"uf('+93^')", "Вернуть расширение из заданного полного пути/имени"},
            {"uf('+94^')", "Вернуть имя диска из заданного полного пути/имени"},
            {"uf('+9L^')", "Проверить наличие файла/корректность URL"},
            {"uf('+9C^')", "Вставить данные из заданного текстового файла"},
            {"uf('+9A^')", "Вернуть размер файла в байтах."},
            {"uf('+9D^')", "Сохранить заданный внутренний двоичный объект в заданном файле"},
            {"uf('+9J^')", "Представить заданный двоичный файл в кодированном виде"},
            {"uf('+9K^')", "Удалить заданный файл"},
            {"uf('Z^')", "Размножение экземпляров"},
            {"uf('M^')", "Отсортировать повторения заданного поля"},
            {"uf('6^')", "Выполнить формат"},
            {"uf('+2^')", "Выполнить внешнее приложение"},
            {"uf('+8^')", "Вызвать функцию из внешней DLL"},
            {"uf('S^')", "Универсальный счетчик"},
            {"uf('R^')", "Генерация случайного числа"},
            {"uf('[^')", "Чистка данных от команд контекстного выделения"},
            {
                "uf('+3G^')",
                "Вывод количества документов, найденных во внешней базе по команде G.\n" +
                "(команда возвращает строку RESULT=[кол-во найденных по запросу документов])"
            },
            {
                "uf('+3S^')",
                "Расформатирует найденные по запросу записи.\n" +
                "Если [количество выводимых записей]=0, то возвращает только количество найденных по запросу документов"
            },
            {
                "uf('+3C^')",
                "Функция введена для оптимизации скорости проверки наличия текста в полнотекстовой базе данных"
            },
            {"uf('+1^')", "Очистить (опустошить) все глобальные переменные"},
            {"uf('+1R^')", "Чтение глобальных переменных"},
            {"uf('+1W^')", "Запись в глобальные переменные"},
            {"uf('+1K^')", "Групповая мультираскодировка переменных"},
            {"uf('+1M^')", "Перемножение двух списков (групп переменных)"},
            {"uf('+1S^')", "Вычитание списков (групп переменных)"},
            {"uf('+1A^')", "Сложение списков (групп переменных)"},
            {"uf('+1G^')", "Исключение неоригинальных значений из группы переменных"},
            {"uf('+1T^')", "Сортировка группы переменных"}
        };

        string[] declarationSnippets =
        {
            "public class ^\n{\n}", "private class ^\n{\n}", "internal class ^\n{\n}",
            "public struct ^\n{\n;\n}", "private struct ^\n{\n;\n}", "internal struct ^\n{\n;\n}",
            "public void ^()\n{\n;\n}", "private void ^()\n{\n;\n}", "internal void ^()\n{\n;\n}",
            "protected void ^()\n{\n;\n}",
            "public ^{ get; set; }", "private ^{ get; set; }", "internal ^{ get; set; }", "protected ^{ get; set; }"
        };

        Style invisibleCharsStyle = new InvisibleCharsRenderer(Pens.Gray);
        Color currentLineColor = Color.FromArgb(100, 210, 210, 255);
        Color changedLineColor = Color.FromArgb(255, 230, 230, 255);


        public PowerfulCSharpEditor()
        {
            InitializeComponent();


            //init menu images
            ComponentResourceManager resources =
                new ComponentResourceManager(typeof(PowerfulCSharpEditor));
            copyToolStripMenuItem.Image = ((Image) (resources.GetObject("copyToolStripButton.Image")));
            cutToolStripMenuItem.Image = ((Image) (resources.GetObject("cutToolStripButton.Image")));
            pasteToolStripMenuItem.Image = ((Image) (resources.GetObject("pasteToolStripButton.Image")));
        }


        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CreateTab(null);
        }

        private Style sameWordsStyle = new MarkerStyle(new SolidBrush(Color.FromArgb(50, Color.Gray)));

        private void CreateTab(string fileName)
        {
            try
            {
                var tb = new FastColoredTextBox();
                tb.Font = new Font("Consolas", 9.75f);
                tb.ContextMenuStrip = cmMain;
                tb.Dock = DockStyle.Fill;
                tb.BorderStyle = BorderStyle.Fixed3D;
                //tb.VirtualSpace = true;
                tb.LeftPadding = 17;
                tb.Language = Language.Pft;
                tb.AddStyle(sameWordsStyle); //same words style
                var tab = new FATabStripItem(fileName != null ? Path.GetFileName(fileName) : "[new]", tb);
                tab.Tag = fileName;
                if (fileName != null)
                    tb.OpenFile(fileName);
                tb.Tag = new TbInfo();
                tsFiles.AddTab(tab);
                tsFiles.SelectedItem = tab;
                tb.Focus();
                tb.DelayedTextChangedInterval = 1000;
                tb.DelayedEventsInterval = 500;
                tb.TextChangedDelayed += new EventHandler<TextChangedEventArgs>(tb_TextChangedDelayed);
                tb.SelectionChangedDelayed += new EventHandler(tb_SelectionChangedDelayed);
                tb.KeyDown += new KeyEventHandler(tb_KeyDown);
                tb.MouseMove += new MouseEventHandler(tb_MouseMove);
                tb.ChangedLineColor = changedLineColor;
                if (btHighlightCurrentLine.Checked)
                    tb.CurrentLineColor = currentLineColor;
                tb.ShowFoldingLines = btShowFoldingLines.Checked;
                tb.HighlightingRangeType = HighlightingRangeType.VisibleRange;
                //create autocomplete popup menu
                AutocompleteMenu popupMenu = new AutocompleteMenu(tb);
                popupMenu.Items.ImageList = ilAutocomplete;
                popupMenu.Opening += new EventHandler<CancelEventArgs>(popupMenu_Opening);
                BuildAutocompleteMenu(popupMenu);
                (tb.Tag as TbInfo).popupMenu = popupMenu;
            }
            catch (Exception ex)
            {
                if (MessageBox.Show(ex.Message, "Error", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) ==
                    DialogResult.Retry)
                    CreateTab(fileName);
            }
        }

        private void Clear()
        {
            _resutlBox.Clear();
            _rtfBox.Clear();
            _htmlBox.DocumentText = string.Empty;
            _warningBox.Clear();
        }

        private void Parse()
        {
            string recordText = _recordBox.Text;
            StringReader reader = new StringReader(recordText);
            _record = PlainText.ReadRecord(reader);

            PftLexer lexer = new PftLexer();
            _tokenList = lexer.Tokenize(CurrentTB.Text);


            PftParser parser = new PftParser(_tokenList);
            _program = parser.Parse();
        }

        private void ParseSelected()
        {
            string recordText = _recordBox.Text;
            StringReader reader = new StringReader(recordText);
            _record = PlainText.ReadRecord(reader);

            PftLexer lexer = new PftLexer();
            _tokenList = lexer.Tokenize(CurrentTB.SelectedText);


            PftParser parser = new PftParser(_tokenList);
            _program = parser.Parse();
        }

        private string ParsePft(string text)
        {
            string[] pftStrings = text.Split(
                new[] {Environment.NewLine},
                StringSplitOptions.None);
            StringBuilder builder = new StringBuilder();
            foreach (string s in pftStrings)
            {
                if (!Regex.IsMatch(s, @"/\*.*$"))
                {
                    builder.Append(s).Append(" ");
                }
            }

            Regex regex = new Regex("[ ]{2,}", RegexOptions.None);
            text = regex.Replace(builder.ToString(), " ");
            return text;
        }

        private void Run()
        {
            PftFormatter formatter = new PftFormatter
            {
                Program = _program
            };


            string result = formatter.FormatRecord(_record)
                    .Replace(@"\par", Environment.NewLine)
                    .Replace(@"\tab", "    ")
                    .Replace(@"\page", Environment.NewLine + Environment.NewLine)
                ;
            _resutlBox.Text = result;
            try
            {
                _rtfBox.Rtf = result;
            }
            catch
            {
                _rtfBox.Text = result;
            }

            if (ReferenceEquals(_htmlBox.Document, null))
            {
                _htmlBox.Navigate("about:blank");
                while (_htmlBox.IsBusy)
                {
                    Application.DoEvents();
                }
            }
            if (!ReferenceEquals(_htmlBox.Document, null))
            {
                _htmlBox.Document.Write(result);
            }
            try
            {
                _htmlBox.DocumentText =
                    "<html>"
                    + result
                    + "</html>";
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch
            {
                // Nothing to do
            }


            _warningBox.Text = formatter.Warning;
        }

        private void _goButton_Click
        (
            object sender,
            EventArgs e
        )
        {
            try
            {
                Clear();
                if (CurrentTB.SelectedText.Equals(""))
                {
                    Parse();
                }
                else
                {
                    ParseSelected();
                }
                Run();
            }
            catch (Exception exception)
            {
                _resutlBox.Text = exception.ToString();
            }
        }

        private static string PftNormalize(string pft)
        {
            pft = Regex.Replace(pft, "then", "then", RegexOptions.IgnoreCase);
            pft = Regex.Replace(pft, "if", "if", RegexOptions.IgnoreCase);
            pft = Regex.Replace(pft, "else", "else", RegexOptions.IgnoreCase);
            pft = Regex.Replace(pft, "fi", "fi", RegexOptions.IgnoreCase);
            pft = pft.Replace(Environment.NewLine, " ");
            Regex regex = new Regex("[ ]{2,}", RegexOptions.None);
            pft = regex.Replace(pft, " ");
            pft = pft
                .Replace("unifor", "uf")
                .Replace("if", "\nif").Replace("else", "\nelse\n").Replace("then", "then\n").Replace("fi,", "fi")
                .Replace("fi", "\nfi,\n")
                .Replace("\n \n", "\n");

            return pft;
        }


        private void MainForm_PreviewKeyDown
        (
            object sender,
            PreviewKeyDownEventArgs e
        )
        {
            switch (e.KeyData)
            {
                case Keys.F5:
                    _goButton_Click(sender, e);
                    break;
            }
            if (e.Modifiers == Keys.Control && e.KeyCode == Keys.N)
            {
                CreateTab(null);
            }

            if (e.Modifiers == Keys.Control && e.KeyCode == Keys.S)
            {
                Save(tsFiles.SelectedItem);
            }
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyData)
            {
                case Keys.F5:
                    _goButton_Click(sender, e);
                    e.Handled = true;
                    break;
            }
            if (e.Modifiers == Keys.Control && e.KeyCode == Keys.N)
            {
                CreateTab(null);
            }
            if (e.Modifiers == Keys.Control && e.KeyCode == Keys.S)
            {
                Save(tsFiles.SelectedItem);
            }
        }


        void popupMenu_Opening(object sender, CancelEventArgs e)
        {
            //---block autocomplete menu for comments
            //get index of green style (used for comments)
            var iGreenStyle = CurrentTB.GetStyleIndex(CurrentTB.SyntaxHighlighter.GreenStyle);
            if (iGreenStyle >= 0)
                if (CurrentTB.Selection.Start.iChar > 0)
                {
                    //current char (before caret)
                    var c = CurrentTB[CurrentTB.Selection.Start.iLine][CurrentTB.Selection.Start.iChar - 1];
                    //green Style
                    var greenStyleIndex = Range.ToStyleIndex(iGreenStyle);
                    //if char contains green style then block popup menu
                    if ((c.style & greenStyleIndex) != 0)
                        e.Cancel = true;
                }
        }

        private void BuildAutocompleteMenu(AutocompleteMenu popupMenu)
        {
            List<AutocompleteItem> items = new List<AutocompleteItem>();

            foreach (var item in snippets)
                items.Add(new SnippetAutocompleteItem(item) {ImageIndex = 1});
            foreach (var pair in _unifors)
            {
                string title = pair.Value.Split('\n')[0];
                List<string> list = new List<string>(pair.Value.Split('\n'));
                list.RemoveAt(0);
                string text = string.Join("\n", list.ToArray());
                items.Add(new SnippetAutocompleteItem(pair.Key, title, text) {ImageIndex = 1});
            }
            foreach (var item in declarationSnippets)
                items.Add(new DeclarationSnippet(item) {ImageIndex = 0});
            foreach (var item in methods)
                items.Add(new MethodAutocompleteItem(item) {ImageIndex = 2});
            foreach (var item in keywords)
                items.Add(new AutocompleteItem(item));

            items.Add(new InsertSpaceSnippet());
            items.Add(new InsertSpaceSnippet(@"^(\w+)([=<>!:]+)(\w+)$"));
            items.Add(new InsertEnterSnippet());

            //set as autocomplete source
            popupMenu.Items.SetAutocompleteItems(items);
            popupMenu.SearchPattern = @"[\w\.:=!<>]";
        }

        void tb_MouseMove(object sender, MouseEventArgs e)
        {
            var tb = sender as FastColoredTextBox;
            var place = tb.PointToPlace(e.Location);
            var r = new Range(tb, place, place);

            string text = r.GetFragment("[a-zA-Z]").Text;
            lbWordUnderMouse.Text = text;
        }

        void tb_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Modifiers == Keys.Control && e.KeyCode == Keys.OemMinus)
            {
                NavigateBackward();
                e.Handled = true;
            }

            if (e.Modifiers == (Keys.Control | Keys.Shift) && e.KeyCode == Keys.OemMinus)
            {
                NavigateForward();
                e.Handled = true;
            }

            if (e.KeyData == (Keys.K | Keys.Control))
            {
                //forced show (MinFragmentLength will be ignored)
                (CurrentTB.Tag as TbInfo).popupMenu.Show(true);
                e.Handled = true;
            }
        }

        void tb_SelectionChangedDelayed(object sender, EventArgs e)
        {
            var tb = sender as FastColoredTextBox;
            //remember last visit time
            if (tb.Selection.IsEmpty && tb.Selection.Start.iLine < tb.LinesCount)
            {
                if (lastNavigatedDateTime != tb[tb.Selection.Start.iLine].LastVisit)
                {
                    tb[tb.Selection.Start.iLine].LastVisit = DateTime.Now;
                    lastNavigatedDateTime = tb[tb.Selection.Start.iLine].LastVisit;
                }
            }

            //highlight same words
            tb.VisibleRange.ClearStyle(sameWordsStyle);
            if (!tb.Selection.IsEmpty)
                return; //user selected diapason
            //get fragment around caret
            var fragment = tb.Selection.GetFragment(@"\w");
            string text = fragment.Text;
            if (text.Length == 0)
                return;
            //highlight same words
            Range[] ranges = tb.VisibleRange.GetRanges("\\b" + text + "\\b").ToArray();

            if (ranges.Length > 1)
                foreach (var r in ranges)
                    r.SetStyle(sameWordsStyle);
        }

        void tb_TextChangedDelayed(object sender, TextChangedEventArgs e)
        {
            FastColoredTextBox tb = (sender as FastColoredTextBox);
            //rebuild object explorer
            string text = (sender as FastColoredTextBox).Text;
            ThreadPool.QueueUserWorkItem(
                (o) => ReBuildObjectExplorer(text)
            );

            //show invisible chars
            HighlightInvisibleChars(e.ChangedRange);
        }

        private void HighlightInvisibleChars(Range range)
        {
            range.ClearStyle(invisibleCharsStyle);
            if (btInvisibleChars.Checked)
                range.SetStyle(invisibleCharsStyle, @".$|.\r\n|\s");
        }

        List<ExplorerItem> explorerList = new List<ExplorerItem>();

        private void ReBuildObjectExplorer(string text)
        {
            try
            {
                List<ExplorerItem> list = new List<ExplorerItem>();
                int lastClassIndex = -1;
                //find classes, methods and properties
                Regex regex =
                    new Regex(
                        @"^(?<range>[\w\s]+\b(class|struct|enum|interface)\s+[\w<>,\s]+)|^\s*(public|private|internal|protected)[^\n]+(\n?\s*{|;)?",
                        RegexOptions.Multiline);
                foreach (Match r in regex.Matches(text))
                    try
                    {
                        string s = r.Value;
                        int i = s.IndexOfAny(new char[] {'=', '{', ';'});
                        if (i >= 0)
                            s = s.Substring(0, i);
                        s = s.Trim();

                        var item = new ExplorerItem() {title = s, position = r.Index};
                        if (Regex.IsMatch(item.title, @"\b(class|struct|enum|interface)\b"))
                        {
                            item.title = item.title.Substring(item.title.LastIndexOf(' ')).Trim();
                            item.type = ExplorerItemType.Class;
                            list.Sort(lastClassIndex + 1, list.Count - (lastClassIndex + 1),
                                new ExplorerItemComparer());
                            lastClassIndex = list.Count;
                        }
                        else if (item.title.Contains(" event "))
                        {
                            int ii = item.title.LastIndexOf(' ');
                            item.title = item.title.Substring(ii).Trim();
                            item.type = ExplorerItemType.Event;
                        }
                        else if (item.title.Contains("("))
                        {
                            var parts = item.title.Split('(');
                            item.title = parts[0].Substring(parts[0].LastIndexOf(' ')).Trim() + "(" + parts[1];
                            item.type = ExplorerItemType.Method;
                        }
                        else if (item.title.EndsWith("]"))
                        {
                            var parts = item.title.Split('[');
                            if (parts.Length < 2) continue;
                            item.title = parts[0].Substring(parts[0].LastIndexOf(' ')).Trim() + "[" + parts[1];
                            item.type = ExplorerItemType.Method;
                        }
                        else
                        {
                            int ii = item.title.LastIndexOf(' ');
                            item.title = item.title.Substring(ii).Trim();
                            item.type = ExplorerItemType.Property;
                        }
                        list.Add(item);
                    }
                    catch
                    {
                        ;
                    }

                list.Sort(lastClassIndex + 1, list.Count - (lastClassIndex + 1), new ExplorerItemComparer());

                BeginInvoke(
                    new Action(() =>
                    {
                        explorerList = list;
                        dgvObjectExplorer.RowCount = explorerList.Count;
                        dgvObjectExplorer.Invalidate();
                    })
                );
            }
            catch
            {
                ;
            }
        }

        enum ExplorerItemType
        {
            Class,
            Method,
            Property,
            Event
        }

        class ExplorerItem
        {
            public ExplorerItemType type;
            public string title;
            public int position;
        }

        class ExplorerItemComparer : IComparer<ExplorerItem>
        {
            public int Compare(ExplorerItem x, ExplorerItem y)
            {
                return x.title.CompareTo(y.title);
            }
        }

        private void tsFiles_TabStripItemClosing(TabStripItemClosingEventArgs e)
        {
            if ((e.Item.Controls[0] as FastColoredTextBox).IsChanged)
            {
                switch (MessageBox.Show("Do you want save " + e.Item.Title + " ?", "Save",
                    MessageBoxButtons.YesNoCancel, MessageBoxIcon.Information))
                {
                    case DialogResult.Yes:
                        if (!Save(e.Item))
                            e.Cancel = true;
                        break;
                    case DialogResult.Cancel:
                        e.Cancel = true;
                        break;
                }
            }
        }

        private bool Save(FATabStripItem tab)
        {
            var tb = (tab.Controls[0] as FastColoredTextBox);
            if (tab.Tag == null)
            {
                if (sfdMain.ShowDialog() != DialogResult.OK)
                    return false;
                tab.Title = Path.GetFileName(sfdMain.FileName);
                tab.Tag = sfdMain.FileName;
            }

            try
            {
                var path = tab.Tag as string;
                File.WriteAllText(path, tb.Text, Encoding.Default);

                if (path.Contains("PFTe"))
                {
                    File.WriteAllText(path.Replace("PFTe", "PFT"), ParsePft(tb.Text), Encoding.Default);
                }
                tb.IsChanged = false;
            }
            catch (Exception ex)
            {
                if (MessageBox.Show(ex.Message, "Error", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) ==
                    DialogResult.Retry)
                    return Save(tab);
                else
                    return false;
            }

            tb.Invalidate();

            return true;
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tsFiles.SelectedItem != null)
                Save(tsFiles.SelectedItem);
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tsFiles.SelectedItem != null)
            {
                string oldFile = tsFiles.SelectedItem.Tag as string;
                tsFiles.SelectedItem.Tag = null;
                if (!Save(tsFiles.SelectedItem))
                    if (oldFile != null)
                    {
                        tsFiles.SelectedItem.Tag = oldFile;
                        tsFiles.SelectedItem.Title = Path.GetFileName(oldFile);
                    }
            }
        }

        private void quitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ofdMain.ShowDialog() == DialogResult.OK)
                CreateTab(ofdMain.FileName);
        }

        FastColoredTextBox CurrentTB
        {
            get
            {
                if (tsFiles.SelectedItem == null)
                    return null;
                return (tsFiles.SelectedItem.Controls[0] as FastColoredTextBox);
            }

            set
            {
                tsFiles.SelectedItem = (value.Parent as FATabStripItem);
                value.Focus();
            }
        }

        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTB.Cut();
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTB.Copy();
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTB.Paste();
        }

        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTB.Selection.SelectAll();
        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CurrentTB.UndoEnabled)
                CurrentTB.Undo();
        }

        private void redoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CurrentTB.RedoEnabled)
                CurrentTB.Redo();
        }

        private void tmUpdateInterface_Tick(object sender, EventArgs e)
        {
            try
            {
                if (CurrentTB != null && tsFiles.Items.Count > 0)
                {
                    var tb = CurrentTB;
                    undoStripButton.Enabled = undoToolStripMenuItem.Enabled = tb.UndoEnabled;
                    redoStripButton.Enabled = redoToolStripMenuItem.Enabled = tb.RedoEnabled;
                    saveToolStripButton.Enabled = saveToolStripMenuItem.Enabled = tb.IsChanged;
                    saveAsToolStripMenuItem.Enabled = true;
                    pasteToolStripButton.Enabled = pasteToolStripMenuItem.Enabled = true;
                    cutToolStripButton.Enabled = cutToolStripMenuItem.Enabled =
                        copyToolStripButton.Enabled = copyToolStripMenuItem.Enabled = !tb.Selection.IsEmpty;
                    printToolStripButton.Enabled = true;
                }
                else
                {
                    saveToolStripButton.Enabled = saveToolStripMenuItem.Enabled = false;
                    saveAsToolStripMenuItem.Enabled = false;
                    cutToolStripButton.Enabled = cutToolStripMenuItem.Enabled =
                        copyToolStripButton.Enabled = copyToolStripMenuItem.Enabled = false;
                    pasteToolStripButton.Enabled = pasteToolStripMenuItem.Enabled = false;
                    printToolStripButton.Enabled = false;
                    undoStripButton.Enabled = undoToolStripMenuItem.Enabled = false;
                    redoStripButton.Enabled = redoToolStripMenuItem.Enabled = false;
                    dgvObjectExplorer.RowCount = 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void printToolStripButton_Click(object sender, EventArgs e)
        {
            if (CurrentTB != null)
            {
                var settings = new PrintDialogSettings();
                settings.Title = tsFiles.SelectedItem.Title;
                settings.Header = "&b&w&b";
                settings.Footer = "&b&p";
                CurrentTB.Print(settings);
            }
        }

        bool tbFindChanged = false;

        private void tbFind_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r' && CurrentTB != null)
            {
                Range r = tbFindChanged ? CurrentTB.Range.Clone() : CurrentTB.Selection.Clone();
                tbFindChanged = false;
                r.End = new Place(CurrentTB[CurrentTB.LinesCount - 1].Count, CurrentTB.LinesCount - 1);
                var pattern = Regex.Escape(tbFind.Text);
                foreach (var found in r.GetRanges(pattern))
                {
                    found.Inverse();
                    CurrentTB.Selection = found;
                    CurrentTB.DoSelectionVisible();
                    return;
                }
                MessageBox.Show("Not found.");
            }
            else
                tbFindChanged = true;
        }

        private void findToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTB.ShowFindDialog();
        }

        private void replaceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTB.ShowReplaceDialog();
        }

        private void PowerfulCSharpEditor_FormClosing(object sender, FormClosingEventArgs e)
        {
            List<FATabStripItem> list = new List<FATabStripItem>();
            foreach (FATabStripItem tab in tsFiles.Items)
                list.Add(tab);
            foreach (var tab in list)
            {
                TabStripItemClosingEventArgs args = new TabStripItemClosingEventArgs(tab);
                tsFiles_TabStripItemClosing(args);
                if (args.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
                tsFiles.RemoveTab(tab);
            }
        }

        private void dgvObjectExplorer_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (CurrentTB != null)
            {
                var item = explorerList[e.RowIndex];
                CurrentTB.GoEnd();
                CurrentTB.SelectionStart = item.position;
                CurrentTB.DoSelectionVisible();
                CurrentTB.Focus();
            }
        }

        private void dgvObjectExplorer_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            try
            {
                ExplorerItem item = explorerList[e.RowIndex];
                if (e.ColumnIndex == 1)
                    e.Value = item.title;
                else
                    switch (item.type)
                    {
                        case ExplorerItemType.Class:
                            e.Value = Resources.class_libraries;
                            return;
                        case ExplorerItemType.Method:
                            e.Value = Resources.box;
                            return;
                        case ExplorerItemType.Event:
                            e.Value = Resources.lightning;
                            return;
                        case ExplorerItemType.Property:
                            e.Value = Resources.property;
                            return;
                    }
            }
            catch
            {
                ;
            }
        }

        private void tsFiles_TabStripItemSelectionChanged(TabStripItemChangedEventArgs e)
        {
            if (CurrentTB != null)
            {
                CurrentTB.Focus();
                string text = CurrentTB.Text;
                ThreadPool.QueueUserWorkItem(
                    (o) => ReBuildObjectExplorer(text)
                );
            }
        }

        private void backStripButton_Click(object sender, EventArgs e)
        {
            NavigateBackward();
        }

        private void forwardStripButton_Click(object sender, EventArgs e)
        {
            NavigateForward();
        }

        DateTime lastNavigatedDateTime = DateTime.Now;

        private bool NavigateBackward()
        {
            DateTime max = new DateTime();
            int iLine = -1;
            FastColoredTextBox tb = null;
            for (int iTab = 0; iTab < tsFiles.Items.Count; iTab++)
            {
                var t = (tsFiles.Items[iTab].Controls[0] as FastColoredTextBox);
                for (int i = 0; i < t.LinesCount; i++)
                    if (t[i].LastVisit < lastNavigatedDateTime && t[i].LastVisit > max)
                    {
                        max = t[i].LastVisit;
                        iLine = i;
                        tb = t;
                    }
            }
            if (iLine >= 0)
            {
                tsFiles.SelectedItem = (tb.Parent as FATabStripItem);
                tb.Navigate(iLine);
                lastNavigatedDateTime = tb[iLine].LastVisit;
                Console.WriteLine("Backward: " + lastNavigatedDateTime);
                tb.Focus();
                tb.Invalidate();
                return true;
            }
            else
                return false;
        }

        private bool NavigateForward()
        {
            DateTime min = DateTime.Now;
            int iLine = -1;
            FastColoredTextBox tb = null;
            for (int iTab = 0; iTab < tsFiles.Items.Count; iTab++)
            {
                var t = (tsFiles.Items[iTab].Controls[0] as FastColoredTextBox);
                for (int i = 0; i < t.LinesCount; i++)
                    if (t[i].LastVisit > lastNavigatedDateTime && t[i].LastVisit < min)
                    {
                        min = t[i].LastVisit;
                        iLine = i;
                        tb = t;
                    }
            }
            if (iLine >= 0)
            {
                tsFiles.SelectedItem = (tb.Parent as FATabStripItem);
                tb.Navigate(iLine);
                lastNavigatedDateTime = tb[iLine].LastVisit;
                Console.WriteLine("Forward: " + lastNavigatedDateTime);
                tb.Focus();
                tb.Invalidate();
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// This item appears when any part of snippet text is typed
        /// </summary>
        class DeclarationSnippet : SnippetAutocompleteItem
        {
            public DeclarationSnippet(string snippet)
                : base(snippet)
            {
            }

            public override CompareResult Compare(string fragmentText)
            {
                var pattern = Regex.Escape(fragmentText);
                if (Regex.IsMatch(Text, "\\b" + pattern, RegexOptions.IgnoreCase))
                    return CompareResult.Visible;
                return CompareResult.Hidden;
            }
        }

        /// <summary>
        /// Divides numbers and words: "123AND456" -> "123 AND 456"
        /// Or "i=2" -> "i = 2"
        /// </summary>
        class InsertSpaceSnippet : AutocompleteItem
        {
            string pattern;

            public InsertSpaceSnippet(string pattern)
                : base("")
            {
                this.pattern = pattern;
            }

            public InsertSpaceSnippet()
                : this(@"^(\d+)([a-zA-Z_]+)(\d*)$")
            {
            }

            public override CompareResult Compare(string fragmentText)
            {
                if (Regex.IsMatch(fragmentText, pattern))
                {
                    Text = InsertSpaces(fragmentText);
                    if (Text != fragmentText)
                        return CompareResult.Visible;
                }
                return CompareResult.Hidden;
            }

            public string InsertSpaces(string fragment)
            {
                var m = Regex.Match(fragment, pattern);
                if (m == null)
                    return fragment;
                if (m.Groups[1].Value == "" && m.Groups[3].Value == "")
                    return fragment;
                return (m.Groups[1].Value + " " + m.Groups[2].Value + " " + m.Groups[3].Value).Trim();
            }

            public override string ToolTipTitle
            {
                get { return Text; }
            }
        }

        /// <summary>
        /// Inerts line break after '}'
        /// </summary>
        class InsertEnterSnippet : AutocompleteItem
        {
            Place enterPlace = Place.Empty;

            public InsertEnterSnippet()
                : base("[Line break]")
            {
            }

            public override CompareResult Compare(string fragmentText)
            {
                var r = Parent.Fragment.Clone();
                while (r.Start.iChar > 0)
                {
                    if (r.CharBeforeStart == '}')
                    {
                        enterPlace = r.Start;
                        return CompareResult.Visible;
                    }

                    r.GoLeftThroughFolded();
                }

                return CompareResult.Hidden;
            }

            public override string GetTextForReplace()
            {
                //extend range
                Range r = Parent.Fragment;
                Place end = r.End;
                r.Start = enterPlace;
                r.End = r.End;
                //insert line break
                return Environment.NewLine + r.Text;
            }

            public override void OnSelected(AutocompleteMenu popupMenu, SelectedEventArgs e)
            {
                base.OnSelected(popupMenu, e);
                if (Parent.Fragment.tb.AutoIndent)
                    Parent.Fragment.tb.DoAutoIndent();
            }

            public override string ToolTipTitle
            {
                get { return "Insert line break after '}'"; }
            }
        }

        private void autoIndentSelectedTextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTB.DoAutoIndent();
        }

        private void btInvisibleChars_Click(object sender, EventArgs e)
        {
            foreach (FATabStripItem tab in tsFiles.Items)
                HighlightInvisibleChars((tab.Controls[0] as FastColoredTextBox).Range);
            if (CurrentTB != null)
                CurrentTB.Invalidate();
        }

        private void btHighlightCurrentLine_Click(object sender, EventArgs e)
        {
            foreach (FATabStripItem tab in tsFiles.Items)
            {
                if (btHighlightCurrentLine.Checked)
                    (tab.Controls[0] as FastColoredTextBox).CurrentLineColor = currentLineColor;
                else
                    (tab.Controls[0] as FastColoredTextBox).CurrentLineColor = Color.Transparent;
            }
            if (CurrentTB != null)
                CurrentTB.Invalidate();
        }

        private void commentSelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTB.InsertLinePrefix("/*");
        }

        private void uncommentSelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTB.RemoveLinePrefix("/*");
        }

        private void cloneLinesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //expand selection
            CurrentTB.Selection.Expand();
            //get text of selected lines
            string text = Environment.NewLine + CurrentTB.Selection.Text;
            //move caret to end of selected lines
            CurrentTB.Selection.Start = CurrentTB.Selection.End;
            //insert text
            CurrentTB.InsertText(text);
        }

        private void cloneLinesAndCommentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //start autoUndo block
            CurrentTB.BeginAutoUndo();
            //expand selection
            CurrentTB.Selection.Expand();
            //get text of selected lines
            string text = Environment.NewLine + CurrentTB.Selection.Text;
            //comment lines
            CurrentTB.InsertLinePrefix("/*");
            //move caret to end of selected lines
            CurrentTB.Selection.Start = CurrentTB.Selection.End;
            //insert text
            CurrentTB.InsertText(text);
            //end of autoUndo block
            CurrentTB.EndAutoUndo();
        }


        private void bookmarkPlusButton_Click(object sender, EventArgs e)
        {
            if (CurrentTB == null)
                return;
            CurrentTB.BookmarkLine(CurrentTB.Selection.Start.iLine);
        }

        private void bookmarkMinusButton_Click(object sender, EventArgs e)
        {
            if (CurrentTB == null)
                return;
            CurrentTB.UnbookmarkLine(CurrentTB.Selection.Start.iLine);
        }

        private void reformatTextButton_Click(object sender, EventArgs e)
        {
            if (CurrentTB == null)
                return;
            CurrentTB.Text = PftNormalize(CurrentTB.Text);
            CurrentTB.SelectAll();
            CurrentTB.DoAutoIndent();
        }

        private void removeFormatButton_Click(object sender, EventArgs e)
        {
            if (CurrentTB == null || CurrentTB.SelectedText.Equals(""))
                return;
            string select = CurrentTB.SelectedText;
            select = select.Replace(Environment.NewLine, " ");
            Regex regex = new Regex("[ ]{2,}", RegexOptions.None);
            select = regex.Replace(select, " ");
            CurrentTB.SelectedText = select;
        }

        private void gotoButton_DropDownOpening(object sender, EventArgs e)
        {
            gotoButton.DropDownItems.Clear();
            foreach (Control tab in tsFiles.Items)
            {
                FastColoredTextBox tb = tab.Controls[0] as FastColoredTextBox;
                foreach (var bookmark in tb.Bookmarks)
                {
                    var item = gotoButton.DropDownItems.Add(
                        bookmark.Name + " [" + Path.GetFileNameWithoutExtension(tab.Tag as String) + "]");
                    item.Tag = bookmark;
                    item.Click += (o, a) =>
                    {
                        var b = (Bookmark) (o as ToolStripItem).Tag;
                        try
                        {
                            CurrentTB = b.TB;
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                            return;
                        }
                        b.DoVisible();
                    };
                }
            }
        }

        private void btShowFoldingLines_Click(object sender, EventArgs e)
        {
            foreach (FATabStripItem tab in tsFiles.Items)
                (tab.Controls[0] as FastColoredTextBox).ShowFoldingLines = btShowFoldingLines.Checked;
            if (CurrentTB != null)
                CurrentTB.Invalidate();
        }

        private void Zoom_click(object sender, EventArgs e)
        {
            if (CurrentTB != null)
                CurrentTB.Zoom = int.Parse((sender as ToolStripItem).Tag.ToString());
        }

        private void TypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            string fileName = this.typeComboBox.Text + ".TXT";
            string path = Path.Combine(Environment.CurrentDirectory, fileName);
            _recordBox.Text = File.ReadAllText(path, Encoding.Default);
        }
    }

    public class InvisibleCharsRenderer : Style
    {
        Pen pen;

        public InvisibleCharsRenderer(Pen pen)
        {
            this.pen = pen;
        }

        public override void Draw(Graphics gr, Point position, Range range)
        {
            var tb = range.tb;
            using (Brush brush = new SolidBrush(pen.Color))
                foreach (var place in range)
                {
                    switch (tb[place].c)
                    {
                        case ' ':
                            var point = tb.PlaceToPoint(place);
                            point.Offset(tb.CharWidth / 2, tb.CharHeight / 2);
                            gr.DrawLine(pen, point.X, point.Y, point.X + 1, point.Y);
                            break;
                    }

                    if (tb[place.iLine].Count - 1 == place.iChar)
                    {
                        var point = tb.PlaceToPoint(place);
                        point.Offset(tb.CharWidth, 0);
                        gr.DrawString("¶", tb.Font, brush, point);
                    }
                }
        }
    }

    public class TbInfo
    {
        public AutocompleteMenu popupMenu;
    }
}