﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections;
using System.Windows.Threading;

using EpgTimer.TunerReserveViewCtrl;

namespace EpgTimer
{
    /// <summary>
    /// TunerReserveMainView.xaml の相互作用ロジック
    /// </summary>
    public partial class TunerReserveMainView : UserControl
    {
        private List<ReserveViewItem> reserveList = new List<ReserveViewItem>();
        private Point clickPos;

        private bool updateReserveData = true;


        public TunerReserveMainView()
        {
            InitializeComponent();

            tunerReserveView.PreviewMouseWheel += new MouseWheelEventHandler(tunerReserveView_PreviewMouseWheel);
            tunerReserveView.ScrollChanged += new ScrollChangedEventHandler(tunerReserveView_ScrollChanged);
            tunerReserveView.LeftDoubleClick += new TunerReserveView.ProgramViewClickHandler(tunerReserveView_LeftDoubleClick);
            tunerReserveView.RightClick += new TunerReserveView.ProgramViewClickHandler(tunerReserveView_RightClick);

        }

        /// <summary>
        /// 保持情報のクリア
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public bool ClearInfo()
        {
            tunerReserveView.ClearInfo();
            tunerReserveTimeView.ClearInfo();
            tunerReserveNameView.ClearInfo();
            reserveList.Clear();

            return true;
        }

        /// <summary>
        /// 表示スクロールイベント呼び出し
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void tunerReserveView_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            try
            {
                if (sender.GetType() == typeof(TunerReserveView))
                {
                    //時間軸の表示もスクロール
                    tunerReserveTimeView.scrollViewer.ScrollToVerticalOffset(tunerReserveView.scrollViewer.VerticalOffset);
                    //サービス名表示もスクロール
                    tunerReserveNameView.scrollViewer.ScrollToHorizontalOffset(tunerReserveView.scrollViewer.HorizontalOffset);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace);
            }
        }

        /// <summary>
        /// マウスホイールイベント呼び出し
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void tunerReserveView_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            try
            {
                e.Handled = true;
                if (sender.GetType() == typeof(TunerReserveView))
                {
                    TunerReserveView view = sender as TunerReserveView;
                    if (Settings.Instance.MouseScrollAuto == true)
                    {
                        view.scrollViewer.ScrollToVerticalOffset(view.scrollViewer.VerticalOffset - e.Delta);
                    }
                    else
                    {
                        if (e.Delta < 0)
                        {
                            //下方向
                            view.scrollViewer.ScrollToVerticalOffset(view.scrollViewer.VerticalOffset + Settings.Instance.ScrollSize);
                        }
                        else
                        {
                            //上方向
                            if (view.scrollViewer.VerticalOffset < Settings.Instance.ScrollSize)
                            {
                                view.scrollViewer.ScrollToVerticalOffset(0);
                            }
                            else
                            {
                                view.scrollViewer.ScrollToVerticalOffset(view.scrollViewer.VerticalOffset - Settings.Instance.ScrollSize);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace);
            }
        }

        /// <summary>
        /// マウス位置から予約情報を取得する
        /// </summary>
        /// <param name="cursorPos">[IN]マウス位置</param>
        /// <returns>nullで存在しない</returns>
        private ReserveData GetReserveItem(Point cursorPos)
        {
            foreach (ReserveViewItem resInfo in reserveList)
            {
                if (resInfo.LeftPos <= cursorPos.X && cursorPos.X < resInfo.LeftPos + resInfo.Width &&
                    resInfo.TopPos <= cursorPos.Y && cursorPos.Y < resInfo.TopPos + resInfo.Height)
                {
                    return resInfo.ReserveInfo;
                }
            }
            return null;
        }

        /// <summary>
        /// 左ボタンダブルクリックイベント呼び出し
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="cursorPos"></param>
        void tunerReserveView_LeftDoubleClick(object sender, Point cursorPos)
        {
            //まず予約情報あるかチェック
            ReserveData reserve = GetReserveItem(cursorPos);
            if (reserve != null)
            {
                //予約変更ダイアログ表示
                ChangeReserve(reserve);
            }
        }

        /// <summary>
        /// 右ボタンクリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="cursorPos"></param>
        void tunerReserveView_RightClick(object sender, Point cursorPos)
        {
            try
            {
                //右クリック表示メニューの作成
                clickPos = cursorPos;
                ReserveData reserve = GetReserveItem(cursorPos);
                if (reserve == null)
                {
                    return;
                }
                ContextMenu menu = new ContextMenu();

                MenuItem menuItemChg = new MenuItem();
                menuItemChg.Header = "予約変更 (_C)";
                MenuItem menuItemChgDlg = new MenuItem();
                menuItemChgDlg.Header = "ダイアログ表示 (_X)";
                menuItemChgDlg.Click += new RoutedEventHandler(cm_chg_Click);

                menuItemChg.Items.Add(menuItemChgDlg);
                menuItemChg.Items.Add(new Separator());

                for (byte i = 0; i < CommonManager.Instance.RecModeList.Length; i++)
                {
                    MenuItem menuItem = new MenuItem();
                    menuItem.Header = CommonManager.Instance.RecModeList[i] + " (_" + i + ")";
                    menuItem.Tag = i;
                    menuItem.Click += new RoutedEventHandler(cm_chg_recmode_Click);
                    menuItem.IsChecked = i == reserve.RecSetting.RecMode;
                    menuItemChg.Items.Add(menuItem);
                }
                menuItemChg.Items.Add(new Separator());

                MenuItem menuItemChgRecPri = new MenuItem();
                menuItemChgRecPri.Header = "優先度 " + reserve.RecSetting.Priority + " (_E)";

                for (byte i = 1; i <= 5; i++)
                {
                    MenuItem menuItem = new MenuItem();
                    menuItem.Header = "_" + i;
                    menuItem.Tag = i;
                    menuItem.Click += new RoutedEventHandler(cm_chg_priority_Click);
                    menuItem.IsChecked = i == reserve.RecSetting.Priority;
                    menuItemChgRecPri.Items.Add(menuItem);
                }
                menuItemChg.Items.Add(menuItemChgRecPri);

                MenuItem menuItemDel = new MenuItem();
                menuItemDel.Header = "予約削除";
                menuItemDel.Click += new RoutedEventHandler(cm_del_Click);

                MenuItem menuItemAutoAdd = new MenuItem();
                menuItemAutoAdd.Header = "自動予約登録";
                menuItemAutoAdd.Click += new RoutedEventHandler(cm_autoadd_Click);
                MenuItem menuItemTimeshift = new MenuItem();
                menuItemTimeshift.Header = "追っかけ再生 (_P)";
                menuItemTimeshift.Click += new RoutedEventHandler(cm_timeShiftPlay_Click);

                menu.Items.Add(menuItemChg);
                menu.Items.Add(menuItemDel);
                menu.Items.Add(menuItemAutoAdd);
                menu.Items.Add(menuItemTimeshift);
                menu.IsOpen = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace);
            }
        }

        /// <summary>
        /// 右クリックメニュー 予約変更クリックイベント呼び出し
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cm_chg_Click(object sender, RoutedEventArgs e)
        {
            ReserveData reserve = GetReserveItem(clickPos);
            if (reserve != null)
            {
                ChangeReserve(reserve);
            }
        }

        /// <summary>
        /// 右クリックメニュー 予約削除クリックイベント呼び出し
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cm_del_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ReserveData reserve = GetReserveItem(clickPos);
                if (reserve == null)
                {
                    return;
                }
                List<UInt32> list = new List<UInt32>();
                list.Add(reserve.ReserveID);
                ErrCode err = CommonManager.CreateSrvCtrl().SendDelReserve(list);
                if (err != ErrCode.CMD_SUCCESS)
                {
                    MessageBox.Show(CommonManager.GetErrCodeText(err) ?? "予約削除でエラーが発生しました。");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace);
            }
        }

        /// <summary>
        /// 右クリックメニュー 予約モード変更イベント呼び出し
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cm_chg_recmode_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ReserveData reserve = GetReserveItem(clickPos);
                if (reserve == null)
                {
                    return;
                }
                reserve.RecSetting.RecMode = (byte)((MenuItem)sender).Tag;
                List<ReserveData> list = new List<ReserveData>();
                list.Add(reserve);
                ErrCode err = CommonManager.CreateSrvCtrl().SendChgReserve(list);
                if (err != ErrCode.CMD_SUCCESS)
                {
                    MessageBox.Show(CommonManager.GetErrCodeText(err) ?? "予約変更でエラーが発生しました。");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace);
            }
        }

        /// <summary>
        /// 右クリックメニュー 優先度変更イベント呼び出し
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cm_chg_priority_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ReserveData reserve = GetReserveItem(clickPos);
                if (reserve == null)
                {
                    return;
                }
                reserve.RecSetting.Priority = (byte)((MenuItem)sender).Tag;
                List<ReserveData> list = new List<ReserveData>();
                list.Add(reserve);
                ErrCode err = CommonManager.CreateSrvCtrl().SendChgReserve(list);
                if (err != ErrCode.CMD_SUCCESS)
                {
                    MessageBox.Show(CommonManager.GetErrCodeText(err) ?? "予約変更でエラーが発生しました。");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace);
            }
        }

        /// <summary>
        /// 右クリックメニュー 自動予約登録イベント呼び出し
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cm_autoadd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender.GetType() != typeof(MenuItem))
                {
                    return;
                }

                ReserveData reserve = GetReserveItem(clickPos);
                if (reserve == null)
                {
                    return;
                }

                SearchWindow dlg = new SearchWindow();
                dlg.Owner = (Window)PresentationSource.FromVisual(this).RootVisual;
                dlg.SetViewMode(1);

                EpgSearchKeyInfo key = new EpgSearchKeyInfo();

                if (reserve.Title != null)
                {
                    key.andKey = reserve.Title;
                }
                Int64 sidKey = ((Int64)reserve.OriginalNetworkID) << 32 | ((Int64)reserve.TransportStreamID) << 16 | ((Int64)reserve.ServiceID);
                key.serviceList.Add(sidKey);

                dlg.SetSearchDefKey(key);
                dlg.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace);
            }
        }

        /// <summary>
        /// 右クリックメニュー 追っかけ再生イベント呼び出し
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cm_timeShiftPlay_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender.GetType() != typeof(MenuItem))
                {
                    return;
                }

                ReserveData reserve = GetReserveItem(clickPos);
                if (reserve == null)
                {
                    return;
                }
                CommonManager.Instance.FilePlay(reserve.ReserveID);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace);
            }
        }

        /// <summary>
        /// 予約変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChangeReserve(ReserveData reserveInfo)
        {
            try
            {
                ChgReserveWindow dlg = new ChgReserveWindow();
                dlg.Owner = (Window)PresentationSource.FromVisual(this).RootVisual;
                dlg.SetReserveInfo(reserveInfo);
                if (dlg.ShowDialog() == true)
                {
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace);
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var ps = PresentationSource.FromVisual(this);
            if (ps != null)
            {
                //高DPI環境でTunerReserveViewの位置を物理ピクセルに合わせるためにヘッダの幅を微調整する
                //RootにUseLayoutRoundingを適用できれば不要だがボタン等が低品質になるので自力でやる
                Point p = grid_container.TransformToVisual(ps.RootVisual).Transform(new Point(40, 40));
                Matrix m = ps.CompositionTarget.TransformToDevice;
                grid_container.ColumnDefinitions[0].Width = new GridLength(40 + Math.Floor(p.X * m.M11) / m.M11 - p.X);
                grid_container.RowDefinitions[0].Height = new GridLength(40 + Math.Floor(p.Y * m.M22) / m.M22 - p.Y);
            }
        }

        /// <summary>
        /// 予約情報更新通知
        /// </summary>
        public void Refresh()
        {
            updateReserveData = true;
            if (this.IsVisible == true)
            {
                ReloadReserveViewItem();
                updateReserveData = false;
            }
        }

        /// <summary>
        /// 予約情報の再描画
        /// </summary>
        private void ReloadReserveViewItem()
        {
            tunerReserveView.ClearInfo();
            tunerReserveTimeView.ClearInfo();
            tunerReserveNameView.ClearInfo();
            var timeList = new List<DateTime>();
            var tunerList = new List<TunerNameViewItem>();
            reserveList.Clear();
            try
            {
                double leftPos = 0;
                foreach (TunerReserveInfo info in CommonManager.Instance.DB.TunerReserveList.Values)
                {
                    double width = 150;
                    int addOffset = reserveList.Count();
                    foreach (uint reserveID in info.reserveList)
                    {
                        ReserveData reserveInfo;
                        if (CommonManager.Instance.DB.ReserveList.TryGetValue(reserveID, out reserveInfo) == false)
                        {
                            continue;
                        }

                        DateTime startTime = reserveInfo.StartTime;
                        DateTime endTime = startTime.AddSeconds(reserveInfo.DurationSecond);
                        if (reserveInfo.RecSetting.UseMargineFlag == 1)
                        {
                            if (reserveInfo.RecSetting.StartMargine < 0)
                            {
                                startTime = startTime.AddSeconds(-reserveInfo.RecSetting.StartMargine);
                            }
                            if (reserveInfo.RecSetting.EndMargine < 0)
                            {
                                endTime = endTime.AddSeconds(reserveInfo.RecSetting.EndMargine);
                            }
                        }

                        var viewItem = new ReserveViewItem(reserveInfo);
                        viewItem.Height = Math.Floor((endTime - startTime).TotalMinutes * Settings.Instance.MinHeight);
                        if (viewItem.Height < Settings.Instance.MinHeight)
                        {
                            viewItem.Height = Settings.Instance.MinHeight;
                        }
                        viewItem.Width = 150;
                        viewItem.LeftPos = leftPos;

                        for (int i = addOffset; i < reserveList.Count; i++)
                        {
                            ReserveData addInfo = reserveList[i].ReserveInfo;
                            DateTime startTimeAdd = addInfo.StartTime;
                            DateTime endTimeAdd = startTimeAdd.AddSeconds(addInfo.DurationSecond);
                            if (addInfo.RecSetting.UseMargineFlag == 1)
                            {
                                if (addInfo.RecSetting.StartMargine < 0)
                                {
                                    startTimeAdd = startTimeAdd.AddSeconds(-addInfo.RecSetting.StartMargine);
                                }
                                if (addInfo.RecSetting.EndMargine < 0)
                                {
                                    endTimeAdd = endTimeAdd.AddSeconds(addInfo.RecSetting.EndMargine);
                                }
                            }

                            if ((startTimeAdd <= startTime && startTime < endTimeAdd) ||
                                (startTimeAdd < endTime && endTime <= endTimeAdd) ||
                                (startTime <= startTimeAdd && startTimeAdd < endTime) ||
                                (startTime < endTimeAdd && endTimeAdd <= endTime)
                                )
                            {
                                if (reserveList[i].LeftPos == viewItem.LeftPos)
                                {
                                    //追加済みのものと重なるので移動して再チェック
                                    i = addOffset - 1;
                                    viewItem.LeftPos += 150;
                                    width = Math.Max(width, viewItem.LeftPos + viewItem.Width - leftPos);
                                }
                            }
                        }

                        reserveList.Add(viewItem);

                        //必要時間リストの構築

                        DateTime chkStartTime = new DateTime(startTime.Year, startTime.Month, startTime.Day, startTime.Hour, 0, 0);
                        while (chkStartTime <= endTime)
                        {
                            int index = timeList.BinarySearch(chkStartTime);
                            if (index < 0)
                            {
                                timeList.Insert(~index, chkStartTime);
                            }
                            chkStartTime = chkStartTime.AddHours(1);
                        }

                    }
                    tunerList.Add(new TunerNameViewItem(info, width));
                    leftPos += width;
                }

                //表示位置設定
                foreach (ReserveViewItem item in reserveList)
                {
                    DateTime startTime = item.ReserveInfo.StartTime;
                    if (item.ReserveInfo.RecSetting.UseMargineFlag == 1)
                    {
                        if (item.ReserveInfo.RecSetting.StartMargine < 0)
                        {
                            startTime = startTime.AddSeconds(-item.ReserveInfo.RecSetting.StartMargine);
                        }
                    }

                    DateTime chkStartTime = new DateTime(startTime.Year,
                        startTime.Month,
                        startTime.Day,
                        startTime.Hour,
                        0,
                        0);
                    int index = timeList.BinarySearch(chkStartTime);
                    if (index >= 0)
                    {
                        item.TopPos = (index * 60 + (startTime - chkStartTime).TotalMinutes) * Settings.Instance.MinHeight;
                    }
                }

                tunerReserveTimeView.SetTime(timeList, true);
                tunerReserveNameView.SetTunerInfo(tunerList);
                tunerReserveView.SetReserveList(reserveList,
                    leftPos,
                    timeList.Count * 60 * Settings.Instance.MinHeight);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace);
            }
        }

        private void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (updateReserveData && IsVisible)
            {
                ReloadReserveViewItem();
                updateReserveData = false;
            }
        }
    }
}
