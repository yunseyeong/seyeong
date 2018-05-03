using Cesco.FW.Global.DBAdapter;
using Cesco.FW.Global.Util.Common;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraScheduler;
using ICPMS;
using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using DevExpress.XtraScheduler.Drawing;
using DevExpress.XtraEditors.Calendar;
using DevExpress.Utils;
using DevExpress.Utils;

namespace 공용장비_스케쥴관리
{
    public partial class CommonDeviceMng : UserControl
    {
        #region 전역변수

        private CesnetUserAuthInfo _userInfo; // 사용자 정보
        private DateTime _selectedDate;
        private DataTable appointmentTable = new DataTable();

        //신청된 데이터
        private string _storeCode = string.Empty;//창고코드
        private string _itemCode = string.Empty;//장비코드
        private string _customerCode = string.Empty;
        private string _customerName = string.Empty;
        private string _usePurpose = string.Empty;
        private string _applicantName = string.Empty;
        private string _deviceName = string.Empty;
        private string _assetNumber = string.Empty;
        private string _Seq = string.Empty;

        /// <summary>
        /// 로그인 사원 정보
        /// </summary>
        private string _strUserID = string.Empty
                , _strDeptCode = string.Empty
                , _strInsertAuth = string.Empty
                , _strUpdateAuth = string.Empty
                , _strDeleteAuth = string.Empty
                , _strSearchAuth = string.Empty
                , _strPrintAuth = string.Empty
                , _strExcelAuth = string.Empty
                , _strDataAuth = string.Empty
                , _strDeptAuth = string.Empty
                , _strStaffAuth = string.Empty
                , _StoreAuthClas = string.Empty;

        private bool _selectDTF = true;
        private bool _selectITF = true;
        private bool _selectSTF = true;
        private bool _viewMonth = true;
        private bool _viewWeek = false;

        private ICPMS.CM_DBCON _dbcon = new ICPMS.CM_DBCON();
        private Cesco.FW.Global.DBAdapter.ConfigurationDetail.DBName _CONNDB = Cesco.FW.Global.DBAdapter.ConfigurationDetail.DBName.DEVELOPDB;
        private CM_CommonFunc _cf = new CM_CommonFunc();

        #endregion 전역변수

        #region 생성자

        public CommonDeviceMng()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="pUserID">사번</param>
        /// <param name="pDeptCode">부서코드</param>
        /// <param name="pInsertAuth">INSERT권한</param>
        /// <param name="pUpdateAuth">UPDATE권한</param>
        /// <param name="pDeleteAuth">DELETE권한</param>
        /// <param name="pSearchAuth">SEARCH권한</param>
        /// <param name="pPrintAuth">PRINT권한</param>
        /// <param name="pExcelAuth">EXCEL저장권한</param>
        /// <param name="pDataAuth">DATA접근권한</param>
        public CommonDeviceMng(string pUserID, string pDeptCode, string pInsertAuth, string pUpdateAuth, string pDeleteAuth, string pSearchAuth, string pPrintAuth, string pExcelAuth, string pDataAuth)
        {
            InitializeComponent();
            _userInfo = new CesnetUserAuthInfo(pUserID, pDeptCode, pInsertAuth, pUpdateAuth, pDeleteAuth, pSearchAuth, pPrintAuth, pExcelAuth, pDataAuth);
            _strUserID = pUserID;
            _strDeptCode = pDeptCode;
            _strInsertAuth = pInsertAuth;
            _strUpdateAuth = pUpdateAuth;
            _strDeleteAuth = pDeleteAuth;
            _strSearchAuth = pSearchAuth;
            _strPrintAuth = pPrintAuth;
            _strExcelAuth = pExcelAuth;
            _strDataAuth = pDataAuth;
        }

        #endregion 생성자

        #region 초기함수

        private void CommonDeviceMng_Load(object sender, EventArgs e)
        {
            Init();
            RefreshCalendar(_storeCode, _itemCode);
        }

        private void Init()
        {
            schedulerControl.WorkDays.BeginUpdate();
            schedulerControl.ActiveViewType = SchedulerViewType.Month;//초기 달력은 월단위로함
            schedulerControl.MonthView.WeekCount = 6;//6주를 보여주도록 함
            schedulerControl.MonthView.CompressWeekend = false;
            schedulerControl.MonthView.AppointmentDisplayOptions.AppointmentAutoHeight = true;
            schedulerControl.MonthView.AppointmentDisplayOptions.StartTimeVisibility = AppointmentTimeVisibility.Never;
            schedulerControl.MonthView.AppointmentDisplayOptions.EndTimeVisibility = AppointmentTimeVisibility.Never;
            
            schedulerControl.WorkDays.Clear();
            schedulerControl.WorkDays.EndUpdate();
            schedulerControl.OptionsView.FirstDayOfWeek = FirstDayOfWeek.Monday;//첫째날은 월요일로 세팅

            DataTable dt = _cf.GetUserInfo(_strUserID, _strDeptCode).Tables[0];
            _strDeptAuth = dt.Rows[0]["DeptAuthClas"].ToString();
            _strStaffAuth = dt.Rows[0]["UserAuthClas"].ToString();
            _StoreAuthClas = dt.Rows[0]["StoreAuthClas"].ToString();

            _selectDTF = _cf.GetAuthList(dt, "Dept");
            _selectITF = _cf.GetAuthList(dt, "Staff");
            _selectSTF = _cf.GetAuthList(dt, "Store");

            uC_DeptCode.Enabled = _selectDTF;
            uC_StoreCode.Enabled = _selectSTF;

            uC_DeptCode.SetDeptList(_strDeptCode, _strUserID, _strDeptAuth, _selectDTF);//부서리스트 세팅
            uC_DeptCode.EditValue = _strDeptCode;//현재 부서로 세팅
            uC_DeptCode.SetVisibleColumn("부서코드");
            cesDateYearMonth.DateTime = DateTime.Now;//년,월의 DEFAULT를 현 날짜로 함

            LUE_ItemList.ItemIndex = 0;

            
        }

        /// <summary>
        /// 기존 신규일정 추가를 삭제
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SchedulerControl_PopupMenuShowing(object sender, PopupMenuShowingEventArgs e)
        {
            e.Menu.Items.Clear();//기존 신규일정 메뉴들 삭제
        }

        #endregion 초기함수

        #region 컨트롤이벤트

        /// <summary>
        /// 캘린더의 뷰를 월단위 주단위로 변경시 라디오버튼 변경
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SchedulerControl_ActiveViewChanged(object sender, EventArgs e)
        {
            if (schedulerControl.ActiveViewType == SchedulerViewType.Month)//캘린더의 뷰가 월단위라면
            {
                RadioBTN_Month.Checked = true;//월단위 라디오버튼 체크
            }
            else//캘딘더의 뷰가 주단위라면
            {
                RadioBTN_Week.Checked = true;//주단위 라디오버튼 체크
            }
        }

        /// <summary>
        /// 장비를 변경할 경우 해당장비의 캘린더로 변경
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LUE_ItemList_EditValueChanged(object sender, EventArgs e)
        {
            _itemCode = LUE_ItemList.EditValue.ToString();//장비코드 세팅
            RefreshCalendar(_storeCode, _itemCode);//달력 새로고침
        }

        /// <summary>
        /// 스케쥴 스타일 변경
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void schedulerControl_AppointmentViewInfoCustomizing(object sender, AppointmentViewInfoCustomizingEventArgs e)
        {
            e.ViewInfo.Appearance.Font = new Font("세스코 R", 9, FontStyle.Regular);//폰트크기 변경
            if(e.ViewInfo.Appointment.Id.Equals(_strUserID))//자신이 등록한 일정이면
                e.ViewInfo.Appearance.BackColor = Color.YellowGreen;//색상변경
            if (schedulerControl.ActiveViewType.Equals(SchedulerViewType.Month))//월단위면
                e.ViewInfo.ToolTipText =e.ViewInfo.Appointment.Description + "\n" + e.ViewInfo.Appointment.Start.ToShortTimeString() + " ~ " + e.ViewInfo.Appointment.End.ToShortTimeString();//시간을 툴팁으로 보여줌
            e.ViewInfo.ShouldShowToolTip = true;
        }

        /// <summary>
        /// 셀 스타일 변경
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void schedulerControl_CustomDrawTimeCell(object sender, CustomDrawObjectEventArgs e)
        {
/*
            if(e.ObjectInfo is TimeCell)//주단위일 경우
                        {
                            TimeCell cell = e.ObjectInfo as TimeCell;
                            DayOfWeek currentDayOfWeek = new DayOfWeek();
                            currentDayOfWeek = cell.Interval.Start.DayOfWeek;
                            if (currentDayOfWeek == DayOfWeek.Sunday)//일요일이면
                            {
                                cell.Appearance.BackColor = Color.PaleVioletRed;//빨간색
                            }
                            else if (currentDayOfWeek == DayOfWeek.Saturday)//토요일이면
                            {
                                cell.Appearance.BackColor = Color.CornflowerBlue;//파란색
                            }
                            cell.Appearance.Font = new Font(this.Font.FontFamily, 52, FontStyle.Bold);//폰트는 BOLD
                        }
                        else if(e.ObjectInfo is MonthSingleWeekCell)//월단위일 경우
                        {
                            MonthSingleWeekCell cell = e.ObjectInfo as MonthSingleWeekCell;
                            DayOfWeek currentDayOfWeek = new DayOfWeek();
                            currentDayOfWeek = cell.Interval.Start.DayOfWeek;
                            if (currentDayOfWeek == DayOfWeek.Sunday)//일요일이면
                            {
                                cell.Appearance.BackColor = Color.PaleVioletRed;//빨간색
                            }
                            else if (currentDayOfWeek == DayOfWeek.Saturday)//토요일이면
                            {
                                cell.Appearance.BackColor = Color.CornflowerBlue;//파란색
                            }
                            cell.Appearance.Font = new Font(this.Font.FontFamily, 52, FontStyle.Bold);//폰트는 BOLD
            
                        }
*/
        }

        /// <summary>
        /// 마우스를 올리면 캘린더가 나오게 함
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dateNavigator_MouseEnter(object sender, EventArgs e)
        {
            dateNavigator.Height = 385;
        }

        /// <summary>
        /// 마우스를 떼면 캘린더가 사라지도록 함
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dateNavigator_MouseLeave(object sender, EventArgs e)
        {
            dateNavigator.Height = 22;
        }

        /// <summary>
        /// 일자를 클릭하면 캘린더의 뷰타입이 Week으로 바뀌는게 deafult라서
        /// WorkWeek으로 설정
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void schedulerControl_DateNavigatorQueryActiveViewType(object sender, DateNavigatorQueryActiveViewTypeEventArgs e)
        {
            if (e.NewViewType.Equals(SchedulerViewType.Week))//Week 타입을
                e.NewViewType = SchedulerViewType.WorkWeek;//WorkWeek 타입으로 변환
        }

        /// <summary>
        /// 클릭이벤트
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dateNavigator_Click(object sender, EventArgs e)
        {
            CalendarHitInfo info = dateNavigator.GetHitInfo(e as MouseEventArgs);
            if (info.InfoType.Equals(CalendarHitInfoType.DecMonth) || info.InfoType.Equals(CalendarHitInfoType.IncMonth))
            {//월을 변경하면
                RefreshCalendar(_storeCode, _itemCode);
                schedulerControl.ActiveViewType = SchedulerViewType.Month;//캘린더의 뷰타입을 월 형식으로 변환
            }
        }

        /// <summary>
        /// 월단위일 경우 폰트 설정
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void schedulerControl_CustomDrawDayOfWeekHeader(object sender, CustomDrawObjectEventArgs e)
        {
            DayOfWeekHeader header = e.ObjectInfo as DayOfWeekHeader;

            if (header.DayOfWeek.Equals(DayOfWeek.Sunday))//일요일이면
            {
                header.Appearance.HeaderCaption.ForeColor = Color.PaleVioletRed;//빨간색
            }
            else if (header.DayOfWeek.Equals(DayOfWeek.Saturday))//토요일이면
            {
                header.Appearance.HeaderCaption.ForeColor = Color.CornflowerBlue;//파란색
            }
            else//그 외에는
            {
                header.Appearance.HeaderCaption.ForeColor = Color.Black;//검정색
            }

            header.Appearance.HeaderCaption.Font = new Font("세스코 R", 9, FontStyle.Bold);//폰트크기 변경
            header.ShouldShowToolTip = true;
        }

        /// <summary>
        /// 주단위일 경우 폰트 설정
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void schedulerControl_CustomDrawDayHeader(object sender, CustomDrawObjectEventArgs e)
        {
            if (schedulerControl.ActiveViewType.Equals(SchedulerViewType.WorkWeek))//WorkWeek 타입일 경우에만 지정
            {
                DayHeader header = e.ObjectInfo as DayHeader;
                if (header.Caption.EndsWith("일요일"))//일요일이면
                    header.Appearance.HeaderCaption.ForeColor = Color.PaleVioletRed;//빨간색
                else if (header.Caption.EndsWith("토요일"))//토요일이면
                    header.Appearance.HeaderCaption.ForeColor = Color.CornflowerBlue;//파란색
                header.Appearance.HeaderCaption.Font = new Font("세스코 R", 9, FontStyle.Bold);//폰트크기 변경
                header.ShouldShowToolTip = true;//툴팁은 항상 보이도록함 (없으면 월단위에서 보이지 않음)
            }
            
        }

        /// <summary>
        /// init tooltip
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolTipController_BeforeShow(object sender, ToolTipControllerShowEventArgs e)
        {
            ToolTipController controller = sender as ToolTipController;
            AppointmentViewInfo aptViewInfo = controller.ActiveObject as AppointmentViewInfo;
            if (aptViewInfo == null) return;
            toolTipController.ToolTipType = ToolTipType.SuperTip;

            if (toolTipController.ToolTipType == ToolTipType.SuperTip)
            {
                SuperToolTip SuperTip = new SuperToolTip();
                SuperToolTipSetupArgs args = new SuperToolTipSetupArgs();
                args.Contents.Text = aptViewInfo.Description;
                args.ShowFooterSeparator = true;
                args.Footer.Text = aptViewInfo.Appointment.Start.ToShortTimeString() + " ~ " + aptViewInfo.Appointment.End.ToShortTimeString();//"SuperTip";
                SuperTip.Setup(args);
                e.SuperTip = SuperTip;
                
            }
            
        }

        private void toolTipController_CustomDraw(object sender, ToolTipControllerCustomDrawEventArgs e)
        {
            
        }

        /// <summary>
        /// datenavigator 폰트 변경
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dateNavigator_CustomDrawDayNumberCell(object sender, CustomDrawDayNumberCellEventArgs e)
        {
            Font font = new Font("세스코 R", 9, FontStyle.Regular);//기본 폰트는 세스코 R
            e.Style.Font = font;
            if (e.Selected)//선택된 날짜는
            {
                e.Style.ForeColor = Color.DarkRed;//빨간색
            }
            else//아니면
            {
                e.Style.ForeColor = Color.Black;//검정색
            }
        }
        private void schedulerControl_AppointmentDrag(object sender, AppointmentDragEventArgs e)
        {
        }

        /// <summary>
        /// 일정의 duration은 그대로 둔 채 시간을 변경하였을 경우
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void schedulerControl_AppointmentDrop(object sender, AppointmentDragEventArgs e)
        {
            if (e.EditedAppointment.Start.Date.CompareTo(DateTime.Now) > 0 || e.EditedAppointment.End.Date.CompareTo(DateTime.Now) > 0)
            {
                if (e.EditedAppointment.Start.Day.Equals(e.SourceAppointment.Start.Day))//일자가 변경되지 않았다면
                {
                    //if (CheckDuplicateSchedule(_assetNumber, e.EditedAppointment.Start.ToString("yyyyMMdd"), e.EditedAppointment.Start.ToString("HHmm"), e.EditedAppointment.End.ToString("HHmm"), e.EditedAppointment.LabelId.ToString()))
                    string start = e.EditedAppointment.Start.ToString("yyyy-MM-dd HH:mm:ss");
                    string end = e.EditedAppointment.End.ToString("yyyy-MM-dd HH:mm:ss");
                    if (CheckDuplicateSchedule(e.EditedAppointment.Description.Split(' ')[0], e.EditedAppointment.Start.ToString("yyyy-MM-dd HH:mm:ss"), start, end, e.EditedAppointment.LabelId.ToString()))
                    {//중복검사
                        EditScheduleTime(e.EditedAppointment.LabelId.ToString(), start, end);
                    }
                    else//스케쥴이 겹칠 경우
                    {
                        XtraMessageBox.Show("해당 자산번호는 입력된 일시에 사용중입니다.", "안내", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }
                }
                else//스케쥴을 하루가 넘길 경우
                {
                    XtraMessageBox.Show("일자를 수정할 수 없습니다", "안내", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
            else
            {
                XtraMessageBox.Show("현시간 이전의 일정을 수정할 수 없습니다", "안내", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        /// <summary>
        /// 일정의 duration을 변경하였을 경우
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void schedulerControl_AppointmentResized(object sender, AppointmentResizeEventArgs e)
        {
            if (e.EditedAppointment.Start.Date.CompareTo(DateTime.Now) > 0 || e.EditedAppointment.End.Date.CompareTo(DateTime.Now) > 0)
            {
                if (e.EditedAppointment.Start.Day.Equals(e.EditedAppointment.End.Day))//시작일과 종료일이 다를수는 없음
                {
                    string start = e.EditedAppointment.Start.ToString("yyyy-MM-dd HH:mm:ss");
                    string end = e.EditedAppointment.End.ToString("yyyy-MM-dd HH:mm:ss");
                    if (CheckDuplicateSchedule(e.EditedAppointment.Description.Split(' ')[0], e.EditedAppointment.Start.ToString("yyyy-MM-dd HH:mm:ss"), start, end, e.EditedAppointment.LabelId.ToString()))
                    {//중복검사
                        EditScheduleTime(e.EditedAppointment.LabelId.ToString(), e.EditedAppointment.Start.ToString("yyyy-MM-dd HH:mm:ss"), e.EditedAppointment.End.ToString("yyyy-MM-dd HH:mm:ss"));
                    }
                    else//스케쥴이 겹칠 경우
                    {
                        XtraMessageBox.Show("해당 자산번호는 입력된 일시에 사용중입니다.", "안내", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }
                }
                else//스케쥴을 하루가 넘길 경우
                {
                    XtraMessageBox.Show("일자를 수정할 수 없습니다.", "안내", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
            else
            {
                XtraMessageBox.Show("현시간 이전의 일정을 수정할 수 없습니다", "안내", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        /// <summary>
        /// 일정을 마우스 이벤트로 변경하였을 경우
        /// </summary>
        /// <param name="pSeq"></param>
        /// <param name="pStart"></param>
        /// <param name="pEnd"></param>
        private void EditScheduleTime(string pSeq, string pStart, string pEnd)
        {
            DataSet ds = new DataSet();
            DBAdapters dbA = new DBAdapters
            {
                LocalInfo = new LocalInfo(_strUserID, System.Reflection.MethodBase.GetCurrentMethod())
            };
            dbA.BindingConfig.ConnectDBName = _CONNDB;
            dbA.Procedure.ProcedureName = "ICPMSDB.dbo.USP_CSN_Set_Edit_CDS_Time";//해당 자산을 입력된 시간에 사용할 수 있는지 확인하는 프로시저
            dbA.Procedure.ParamAdd("SEQ", pSeq);
            dbA.Procedure.ParamAdd("시작일시", pStart);
            dbA.Procedure.ParamAdd("종료일시", pEnd);
            try
            {
                Cursor = Cursors.WaitCursor;
                ds = dbA.ProcedureToDataSet();
                XtraMessageBox.Show("저장이 완료되었습니다.", "안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception e)
            {
                XtraMessageBox.Show(e.Message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        /// <summary>
        /// 입력된 자산이 입력된 일시에 사용중인지 체크
        /// </summary>
        /// <param name="assetNumber">자산번호</param>
        /// <param name="yearMonth">년월</param>
        /// <param name="startHM">시작시각</param>
        /// <param name="endHM">종료시각</param>
        /// <returns>사용중이 아니라면 true, 사용중이라면 false</returns>
        private bool CheckDuplicateSchedule(string assetNumber, string yearMonth, string startHM, string endHM, string pSeq)
        {
            DataSet ds = new DataSet();
            DBAdapters dbA = new DBAdapters
            {
                LocalInfo = new LocalInfo(_strUserID, System.Reflection.MethodBase.GetCurrentMethod())
            };
            dbA.BindingConfig.ConnectDBName = _CONNDB;
            dbA.Procedure.ProcedureName = "ICPMSDB.dbo.USP_CSN_Assets_Check_Schedule";//해당 자산을 입력된 시간에 사용할 수 있는지 확인하는 프로시저
            dbA.Procedure.ParamAdd("자산번호", assetNumber);
            dbA.Procedure.ParamAdd("시작일", yearMonth);
            dbA.Procedure.ParamAdd("시작시간", startHM);
            dbA.Procedure.ParamAdd("종료시간", endHM);
            dbA.Procedure.ParamAdd("SEQ", pSeq);
            try
            {
                Cursor = Cursors.WaitCursor;
                ds = dbA.ProcedureToDataSet();
                if (ds.Tables.Count > 0)//조회된 테이블이 있다면
                {
                    if (ds.Tables[0].Rows.Count > 0)//테이블의 행수가 0보다 크다면
                    {
                        if (Convert.ToInt32(ds.Tables[0].Rows[0]["return"]) > 0)//겹치는게 있으면
                            return false;
                        else//겹치는게 없으면
                            return true;
                    }
                    else//테이블의 행수가 0보다 작으면
                        return true;
                }
                else//조회된 테이블이 없다면
                    return true;
            }
            catch (Exception e)
            {
                XtraMessageBox.Show(e.Message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }
        
        /// <summary>
        /// 일정을 변경하였을 경우
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void schedulerStorage_AppointmentsChanged(object sender, PersistentObjectsEventArgs e)
        {
            if (schedulerControl.Services.SchedulerState.IsDataRefreshAllowed)//refresh able이면
            {
                RefreshCalendar(_storeCode, _itemCode);//refresh
            }
            else//refresh 불가면
                timer1.Start();//타이머 시작
        }

        /// <summary>
        /// schedulerControl 12버전에서는 refreshData가 able, disable가 번갈아 오기에
        /// able일때 refresh
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (schedulerControl.Services.SchedulerState.IsDataRefreshAllowed)//able이면
            {
                RefreshCalendar(_storeCode, _itemCode);//갱신
                timer1.Stop();//타이머 멈춤
            }
        }

        private void dateNavigator_EditDateModified(object sender, EventArgs e)
        {/*
            CalendarHitInfo info = dateNavigator.GetHitInfo(e as MouseEventArgs);
            if (info.InfoType.Equals(CalendarHitInfoType.DecMonth) || info.InfoType.Equals(CalendarHitInfoType.IncMonth))
            {//월을 변경하면
                RefreshCalendar(_storeCode, _itemCode);
                schedulerControl.ActiveViewType = SchedulerViewType.Month;//캘린더의 뷰타입을 월 형식으로 변환
            }*/
            RefreshCalendar(_storeCode, _itemCode);
            schedulerControl.ActiveViewType = SchedulerViewType.Month;//캘린더의 뷰타입을 월 형식으로 변환
        }

        private void schedulerControl_VisibleIntervalChanged(object sender, EventArgs e)
        {
                //RefreshCalendar(_storeCode, _itemCode);
                //schedulerControl.ActiveViewType = SchedulerViewType.Month;//캘린더의 뷰타입을 월 형식으로 변환
        }


        /// <summary>
        /// 월단위 라디오버튼 이벤트
        /// </summary>
        /// <param name="sender"></param>1
        /// <param name="e"></param>
        private void RadioBTN_Month_CheckedChanged(object sender, EventArgs e)
        {
            if (RadioBTN_Month.Checked)//월단위가 체크되어있다면
            {
                _viewMonth = true;
                _viewWeek = false;
            }
            else//월단위가 체크되지 않았다면
            {
                _viewMonth = false;
                _viewWeek = true;
            }
            SetView();//캘린더 변환
        }

        /// <summary>
        /// 주단위 라디오버튼 이벤트
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RadioBTN_Week_CheckedChanged(object sender, EventArgs e)
        {
            if (RadioBTN_Week.Checked)//주단위가 체크되었다면
            {
                _viewMonth = false;
                _viewWeek = true;
            }
            else//주단위가 체크되지 않았다면
            {
                _viewMonth = true;
                _viewWeek = false;
            }
            SetView();//캘린더 변환
        }

        /// <summary>
        /// 부서 변경 이벤트
        /// </summary>
        /// <param name="pDeptCode">부서코드</param>
        private void UC_DeptCode_EditValueChanged(string pDeptCode)
        {
            if (this.uC_DeptCode.EditValue == null)//부서코드가 없다면 리턴
            {
                return;
            }
            else//부서코드가 있다면 해당 사원의 창고권한을 세팅하고 리스트를 가져옴
            {
                uC_StoreCode.SetAuthStoreCode(_strDeptCode, _strUserID, _StoreAuthClas, uC_DeptCode.EditValue.ToString(), false, "Y", "");
                uC_StoreCode.ItemIndex = 0;
            }
        }

        /// <summary>
        /// 창고변경 이벤트
        /// </summary>
        /// <param name="pStoreCode">창고코드</param>
        private void UC_StoreCode_EditValueChanged(string pStoreCode)
        {
            if (this.uC_StoreCode.EditValue == null || pStoreCode.Equals(string.Empty))//창고코드가 없다면 리턴
            {
                return;
            }
            else//창고번호가 있다면
            {
                GetItemList(pStoreCode);//장비의 리스트를 가져옴
            }
        }

        /// <summary>
        /// 캘린더를 더블클릭했을경우 월단위일때는 주단위로 변경, 주단위일때는 신규일정 추가화면 호출
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SchedulerControl_DoubleClick(object sender, EventArgs e)
        {
            _selectedDate = schedulerControl.ActiveView.SelectedInterval.Start;
            if (schedulerControl.ActiveViewType == SchedulerViewType.Month)
            {
                if (uC_StoreCode.Text.Equals(string.Empty))//창고를 선택하지 않았다면 에러
                {
                    XtraMessageBox.Show("창고를 선택해주세요.", "안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    uC_StoreCode.Focus();//창고선택 포커싱
                }
                else if (LUE_ItemList.Text.Equals(string.Empty))//장비를 선택하지 않았다면 에러
                {
                    XtraMessageBox.Show("장비를 선택해주세요.", "안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LUE_ItemList.Focus(); //장비선택 포커싱
                }
                else//모두 입력이 되었다면
                {
                    schedulerControl.ActiveViewType = SchedulerViewType.WorkWeek;//주단위로 변경
                }
            }
        }

        /// <summary>
        /// 년, 월 변경시 캘린더의 형태를 월단위로 바꿈
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="date">선택된 일자</param>
        private void CesDateYearMonth_EditValueChanged(object sender, DateTime date)
        {
            _viewMonth = true;
            _viewWeek = false;
            SetView();
            date = new DateTime(date.Year, date.Month, 1);//해당 년월의 1일로 세팅
            schedulerControl.Start = date;//SchedulerContrl의 시작을 현재 년, 월의 1일로 설정
            RefreshCalendar(_storeCode, _itemCode);//캘린더 새로고침
        }

        #endregion 컨트롤이벤트

        #region 이벤트에 필요한 함수

        /// <summary>
        /// 일정의 시퀀스 번호를 받아옴
        /// </summary>
        /// <param name="pSeq">시퀀스값</param>
        public void GetRequestData(string pSeq)
        {
            _Seq = pSeq;
        }

        /// <summary>
        /// 캘린더를 월단위로 볼지 주단위로 볼지 판단
        /// </summary>
        private void SetView()
        {
            if (_viewMonth)//월단위가 TRUE이면
            {
                schedulerControl.ActiveViewType = SchedulerViewType.Month; //캘린더를 월단위로 바꿈
            }
            else if (_viewWeek)//주단위가 TRUE이면
            {
                schedulerControl.ActiveViewType = SchedulerViewType.WorkWeek; //캘린더를 주단위로 바꿈
            }
        }

        /// <summary>
        /// 일정추가 화면 호출
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SchedulerControl_EditAppointmentFormShowing(object sender, AppointmentFormEventArgs e)
        {
            e.Handled = true;
            if (schedulerControl.ActiveViewType == SchedulerViewType.WorkWeek || schedulerControl.ActiveViewType == SchedulerViewType.Day) //달력이 형태가 주단위일 경우
            {
                if (schedulerControl.SelectedAppointments.Count > 0) //존재하는 일정을 클릭했을 경우
                {
                    _selectedDate = schedulerControl.SelectedInterval.Start;
                    DeviceRequest deviceRequest = new DeviceRequest(_userInfo, e.Appointment, uC_DeptCode.EditValue.ToString());
                    deviceRequest.DataSendEvent += new DeviceRequest.DeviceRequest_EventHandler(GetRequestData);
                    deviceRequest.ShowDialog();//일정추가 화면 호출
                    RefreshCalendar(_storeCode, _itemCode);//캘린더 갱신
                }
                else //새 일정을 추가할 경우
                {
                    if (DateTime.Now.CompareTo(schedulerControl.ActiveView.SelectedInterval.Start) < 0)//이전시간에는 신청불가
                    {
                        _selectedDate = schedulerControl.SelectedInterval.Start;
                        DeviceRequest deviceRequest = new DeviceRequest(e.Appointment.LabelId.ToString(), _selectedDate, _itemCode, LUE_ItemList.Text, _storeCode, _userInfo, uC_DeptCode.EditValue.ToString());
                        deviceRequest.DataSendEvent += new DeviceRequest.DeviceRequest_EventHandler(GetRequestData);
                        deviceRequest.ShowDialog();//일정추가 화면 호출
                        RefreshCalendar(_storeCode, _itemCode);//캘린더 갱신
                    }
                    else
                    {
                        XtraMessageBox.Show("현시간 이전에는 새 일정을 추가할 수 없습니다.", "안내", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// 해당 창고의 장비리스트를 가져옴
        /// </summary>
        /// <param name="whscd">창고코드</param>
        private void GetItemList(string whscd)
        {
            _storeCode = uC_StoreCode.EditValue.ToString();
            DataSet ds = new DataSet();
            DBAdapters dbA = new DBAdapters
            {
                LocalInfo = new LocalInfo(_strUserID, System.Reflection.MethodBase.GetCurrentMethod())
            };
            dbA.BindingConfig.ConnectDBName = _CONNDB;
            dbA.Procedure.ProcedureName = "ICPMSDB.dbo.Usp_CSN_Get_ItemList";//해당 창고의 장비리스트를 가져오는 프로시저
            dbA.Procedure.ParamAdd("WHSCD", whscd);
            //dbA.Procedure.ParamAdd("DEPTCD", _strDeptCode);

            try
            {
                Cursor = Cursors.WaitCursor;
                ds = dbA.ProcedureToDataSet();
                if (ds.Tables[0] == null || ds.Tables[0].Rows.Count == 0)//조회된 내용이 없으면
                {//경고 후 리턴
                    XtraMessageBox.Show("장비의 조회된 내역이 없습니다.", "안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                LUE_ItemList.DataBindings.Clear();
                LUE_ItemList.Properties.Columns.Clear();
                LUE_ItemList.Properties.Columns.Add(new LookUpColumnInfo("ITEMKNM", "장비명"));
                LUE_ItemList.Properties.Columns.Add(new LookUpColumnInfo("ITEMCD", "장비코드"));
                LUE_ItemList.Properties.Columns["ITEMCD"].Visible = false;
                LUE_ItemList.Properties.DisplayMember = "ITEMKNM";
                LUE_ItemList.Properties.ValueMember = "ITEMCD";
                LUE_ItemList.Properties.DataSource = ds.Tables[0].DefaultView;
                RefreshCalendar(_storeCode, _itemCode);//캘린더 갱신
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(ex.Message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        /// <summary>
        /// 캘린더 새로고침
        /// </summary>
        /// <param name="storeCode">창고코드</param>
        /// <param name="itemCode">장비코드</param>
        private void RefreshCalendar(string storeCode, string itemCode)
        {
            DataSet ds = new DataSet();
            DBAdapters dbA = new DBAdapters
            {
                LocalInfo = new LocalInfo(_strUserID, System.Reflection.MethodBase.GetCurrentMethod())
            };
            dbA.BindingConfig.ConnectDBName = _CONNDB;
            dbA.Procedure.ProcedureName = "ICPMSDB.dbo.Usp_CSN_Get_CommonDeviceSchedule";//자산 스케쥴을 가져오는 프로시저
            dbA.Procedure.ParamAdd("창고코드", storeCode);
            dbA.Procedure.ParamAdd("장비코드", itemCode);
            //string yearMonth = dateNavigator.DateTime.ToShortDateString();
            //yearMonth = yearMonth.Replace("-", "").Substring(0, 6);
            //dbA.Procedure.ParamAdd("조회년월", yearMonth);

            try
            {
                Cursor = Cursors.WaitCursor;
                ds = dbA.ProcedureToDataSet();
                //schedulerStorage.Appointments.ResourceSharing = true;
                schedulerStorage.Appointments.DataSource = null;
                schedulerStorage.Appointments.DataSource = ds.Tables[0];
                schedulerStorage.Appointments.Mappings.Start = ds.Tables[0].Columns["시작일시"].ToString();
                schedulerStorage.Appointments.Mappings.End = ds.Tables[0].Columns["종료일시"].ToString();
                schedulerStorage.Appointments.Mappings.Subject = ds.Tables[0].Columns["고객명"].ToString();
                schedulerStorage.Appointments.Mappings.Location = ds.Tables[0].Columns["고객코드"].ToString();
                schedulerStorage.Appointments.Mappings.Description = ds.Tables[0].Columns["DESCRIPTION"].ToString();
                schedulerStorage.Appointments.Mappings.Label = ds.Tables[0].Columns["SEQ"].ToString();
                schedulerStorage.Appointments.Mappings.AppointmentId = ds.Tables[0].Columns["신청자"].ToString();
                _usePurpose = ds.Tables[0].Columns["사용목적"].ToString();
                schedulerControl.Storage = schedulerStorage;
                if (schedulerControl.Services.SchedulerState.IsDataRefreshAllowed)
                    schedulerControl.RefreshData();
                
            }
            catch (Exception e)
            {
                XtraMessageBox.Show(e.Message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        #endregion 이벤트에 필요한 함수
    }
}