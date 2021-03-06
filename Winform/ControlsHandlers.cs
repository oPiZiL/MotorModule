﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Windows.Forms;

/// <summary>
/// Inclusion of PEAK PCAN-Basic namespace
/// </summary>
using Peak.Can.Basic;
using TPCANHandle = System.UInt16;
using TPCANBitrateFD = System.String;
using TPCANTimestampFD = System.UInt64;

namespace ICDIBasic
{
    public partial class MainForm
    {
        bool IsCheckConnection = false;
        public static bool IsDataRecieved = false;
        public static short IDName = 0;

        #region ComboBox event-handlers
        private void cbbChannel_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool bNonPnP;
            string strTemp;

            // Get the handle fromt he text being shown
            //
            strTemp = cbbChannel.Text;
            strTemp = strTemp.Substring(strTemp.IndexOf('(') + 1, 3);

            strTemp = strTemp.Replace('h', ' ').Trim(' ');

            // Determines if the handle belong to a No Plug&Play hardware 
            //
            m_PcanHandle = Convert.ToUInt16(strTemp, 16);
            bNonPnP = m_PcanHandle <= PCANBasic.PCAN_DNGBUS1;
            // Activates/deactivates configuration controls according with the 
            // kind of hardware
            //
            cbbHwType.Enabled = bNonPnP;
            cbbIO.Enabled = bNonPnP;
            cbbInterrupt.Enabled = bNonPnP;
        }

        private void cbbBaudrates_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Saves the current selected baudrate register code
            //
            switch (cbbBaudrates.SelectedIndex)
            {
                case 0:
                    m_Baudrate = TPCANBaudrate.PCAN_BAUD_1M;
                    break;
                case 1:
                    m_Baudrate = TPCANBaudrate.PCAN_BAUD_800K;
                    break;
                case 2:
                    m_Baudrate = TPCANBaudrate.PCAN_BAUD_500K;
                    break;
                case 3:
                    m_Baudrate = TPCANBaudrate.PCAN_BAUD_250K;
                    break;
                case 4:
                    m_Baudrate = TPCANBaudrate.PCAN_BAUD_125K;
                    break;
                case 5:
                    m_Baudrate = TPCANBaudrate.PCAN_BAUD_100K;
                    break;
                case 6:
                    m_Baudrate = TPCANBaudrate.PCAN_BAUD_95K;
                    break;
                case 7:
                    m_Baudrate = TPCANBaudrate.PCAN_BAUD_83K;
                    break;
                case 8:
                    m_Baudrate = TPCANBaudrate.PCAN_BAUD_50K;
                    break;
                case 9:
                    m_Baudrate = TPCANBaudrate.PCAN_BAUD_47K;
                    break;
                case 10:
                    m_Baudrate = TPCANBaudrate.PCAN_BAUD_33K;
                    break;
                case 11:
                    m_Baudrate = TPCANBaudrate.PCAN_BAUD_20K;
                    break;
                case 12:
                    m_Baudrate = TPCANBaudrate.PCAN_BAUD_10K;
                    break;
                case 13:
                    m_Baudrate = TPCANBaudrate.PCAN_BAUD_5K;
                    break;
            }
        }

        private void cbbHwType_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Saves the current type for a no-Plug&Play hardware
            //
            switch (cbbHwType.SelectedIndex)
            {
                case 0:
                    m_HwType = TPCANType.PCAN_TYPE_ISA;
                    break;
                case 1:
                    m_HwType = TPCANType.PCAN_TYPE_ISA_SJA;
                    break;
                case 2:
                    m_HwType = TPCANType.PCAN_TYPE_ISA_PHYTEC;
                    break;
                case 3:
                    m_HwType = TPCANType.PCAN_TYPE_DNG;
                    break;
                case 4:
                    m_HwType = TPCANType.PCAN_TYPE_DNG_EPP;
                    break;
                case 5:
                    m_HwType = TPCANType.PCAN_TYPE_DNG_SJA;
                    break;
                case 6:
                    m_HwType = TPCANType.PCAN_TYPE_DNG_SJA_EPP;
                    break;
            }
        }

        private void cbbParameter_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Activates/deactivates controls according with the selected 
            // PCAN-Basic parameter 
            //
            rdbParamActive.Enabled = cbbParameter.SelectedIndex != 0;
            rdbParamInactive.Enabled = rdbParamActive.Enabled;
            nudDeviceId.Enabled = !rdbParamActive.Enabled;
        }
        #endregion

        #region Button event-handlers
        private void btnHwRefresh_Click(object sender, EventArgs e)
        {
            UInt32 iBuffer;
            TPCANStatus stsResult;
            bool isFD;

            // Clears the Channel combioBox and fill it again with 
            // the PCAN-Basic handles for no-Plug&Play hardware and
            // the detected Plug&Play hardware
            //
            cbbChannel.Items.Clear();
            try
            {
                for (int i = 0; i < m_HandlesArray.Length; i++)
                {
                    // Includes all no-Plug&Play Handles
                    if (m_HandlesArray[i] <= PCANBasic.PCAN_DNGBUS1)
                        cbbChannel.Items.Add(FormatChannelName(m_HandlesArray[i]));
                    else
                    {
                        // Checks for a Plug&Play Handle and, according with the return value, includes it
                        // into the list of available hardware channels.
                        //
                        stsResult = PCANBasic.GetValue(m_HandlesArray[i], TPCANParameter.PCAN_CHANNEL_CONDITION, out iBuffer, sizeof(UInt32));
                        if ((stsResult == TPCANStatus.PCAN_ERROR_OK) && ((iBuffer & PCANBasic.PCAN_CHANNEL_AVAILABLE) == PCANBasic.PCAN_CHANNEL_AVAILABLE))
                        {
                            stsResult = PCANBasic.GetValue(m_HandlesArray[i], TPCANParameter.PCAN_CHANNEL_FEATURES, out iBuffer, sizeof(UInt32));
                            isFD = (stsResult == TPCANStatus.PCAN_ERROR_OK) && ((iBuffer & PCANBasic.FEATURE_FD_CAPABLE) == PCANBasic.FEATURE_FD_CAPABLE);
                            cbbChannel.Items.Add(FormatChannelName(m_HandlesArray[i], isFD));
                        }
                    }
                }
                cbbChannel.SelectedIndex = cbbChannel.Items.Count - 1;
                btnInit.Enabled = cbbChannel.Items.Count > 0;
            }
            catch (DllNotFoundException)
            {
                MessageBox.Show("Unable to find the library: PCANBasic.dll !", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(-1);
            }
        }

        private bool InitPcan()
        {
            TPCANStatus stsResult;

            // Connects a selected PCAN-Basic channel
            //
            if (m_IsFD)
                stsResult = PCANBasic.InitializeFD(
                    m_PcanHandle,
                    txtBitrate.Text);
            else
                m_Baudrate = TPCANBaudrate.PCAN_BAUD_1M;
            stsResult = PCANBasic.Initialize(
                m_PcanHandle,
                m_Baudrate,
                0,
                0,
                0);

            if (stsResult != TPCANStatus.PCAN_ERROR_OK)
                if (stsResult != TPCANStatus.PCAN_ERROR_CAUTION)
                    MessageBox.Show(GetFormatedError(stsResult));
                else
                {
                    IncludeTextMessage("******************************************************");
                    IncludeTextMessage("The bitrate being used is different than the given one");
                    IncludeTextMessage("******************************************************");
                    stsResult = TPCANStatus.PCAN_ERROR_OK;
                }
            else
                // Prepares the PCAN-Basic's PCAN-Trace file
                //
                ConfigureTraceFile();

            // Sets the connection status of the main-form
            //
            SetConnectionStatus(stsResult == TPCANStatus.PCAN_ERROR_OK);
            if (stsResult == TPCANStatus.PCAN_ERROR_OK)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void btnRelease_Click(object sender, EventArgs e)
        {
            // Releases a current connected PCAN-Basic channel
            //
            PCANBasic.Uninitialize(m_PcanHandle);
            //tMMainFormRefresh.Enabled = false;
            //if (m_ProcessThread != null)
            //{
            //    m_ProcessThread.Abort();
            //    m_ProcessThread.Join();
            //    m_ProcessThread = null;
            //}

            // Sets the connection status of the main-form
            //
            SetConnectionStatus(false);
        }

        private void btnFilterApply_Click(object sender, EventArgs e)
        {
            UInt32 iBuffer;
            TPCANStatus stsResult;

            // Gets the current status of the message filter
            //
            if (!GetFilterStatus(out iBuffer))
                return;

            // Configures the message filter for a custom range of messages
            //
            if (rdbFilterCustom.Checked)
            {
                // Sets the custom filter
                //
                stsResult = PCANBasic.FilterMessages(
                m_PcanHandle,
                Convert.ToUInt32(nudIdFrom.Value),
                Convert.ToUInt32(nudIdTo.Value),
                chbFilterExt.Checked ? TPCANMode.PCAN_MODE_EXTENDED : TPCANMode.PCAN_MODE_STANDARD);
                // If success, an information message is written, if it is not, an error message is shown
                //
                if (stsResult == TPCANStatus.PCAN_ERROR_OK)
                    IncludeTextMessage(string.Format("The filter was customized. IDs from {0:X} to {1:X} will be received", nudIdFrom.Text, nudIdTo.Text));
                else
                    MessageBox.Show(GetFormatedError(stsResult));

                return;
            }

            // The filter will be full opened or complete closed
            //
            if (rdbFilterClose.Checked)
                iBuffer = PCANBasic.PCAN_FILTER_CLOSE;
            else
                iBuffer = PCANBasic.PCAN_FILTER_OPEN;

            // The filter is configured
            //
            stsResult = PCANBasic.SetValue(
                m_PcanHandle,
                TPCANParameter.PCAN_MESSAGE_FILTER,
                ref iBuffer,
                sizeof(UInt32));

            // If success, an information message is written, if it is not, an error message is shown
            //
            if (stsResult == TPCANStatus.PCAN_ERROR_OK)
                IncludeTextMessage(string.Format("The filter was successfully {0}", rdbFilterClose.Checked ? "closed." : "opened."));
            else
                MessageBox.Show(GetFormatedError(stsResult));
        }

        private void btnFilterQuery_Click(object sender, EventArgs e)
        {
            UInt32 iBuffer;

            // Queries the current status of the message filter
            //
            if (GetFilterStatus(out iBuffer))
            {
                switch (iBuffer)
                {
                    // The filter is closed
                    //
                    case PCANBasic.PCAN_FILTER_CLOSE:
                        IncludeTextMessage("The Status of the filter is: closed.");
                        break;
                    // The filter is fully opened
                    //
                    case PCANBasic.PCAN_FILTER_OPEN:
                        IncludeTextMessage("The Status of the filter is: full opened.");
                        break;
                    // The filter is customized
                    //
                    case PCANBasic.PCAN_FILTER_CUSTOM:
                        IncludeTextMessage("The Status of the filter is: customized.");
                        break;
                    // The status of the filter is undefined. (Should never happen)
                    //
                    default:
                        IncludeTextMessage("The Status of the filter is: Invalid.");
                        break;
                }
            }
        }

        private void btnParameterSet_Click(object sender, EventArgs e)
        {
            TPCANStatus stsResult;
            UInt32 iBuffer;
            bool bActivate;

            bActivate = rdbParamActive.Checked;

            // Sets a PCAN-Basic parameter value
            //
            switch (cbbParameter.SelectedIndex)
            {
                // The Device-Number of an USB channel will be set
                //
                case 0:
                    iBuffer = Convert.ToUInt32(nudDeviceId.Value);
                    stsResult = PCANBasic.SetValue(m_PcanHandle, TPCANParameter.PCAN_DEVICE_NUMBER, ref iBuffer, sizeof(UInt32));
                    if (stsResult == TPCANStatus.PCAN_ERROR_OK)
                        IncludeTextMessage("The desired Device-Number was successfully configured");
                    break;
                // The 5 Volt Power feature of a PC-card or USB will be set
                //
                case 1:
                    iBuffer = (uint)(bActivate ? PCANBasic.PCAN_PARAMETER_ON : PCANBasic.PCAN_PARAMETER_OFF);
                    stsResult = PCANBasic.SetValue(m_PcanHandle, TPCANParameter.PCAN_5VOLTS_POWER, ref iBuffer, sizeof(UInt32));
                    if (stsResult == TPCANStatus.PCAN_ERROR_OK)
                        IncludeTextMessage(string.Format("The USB/PC-Card 5 power was successfully {0}", bActivate ? "activated" : "deactivated"));
                    break;
                // The feature for automatic reset on BUS-OFF will be set
                //
                case 2:
                    iBuffer = (uint)(bActivate ? PCANBasic.PCAN_PARAMETER_ON : PCANBasic.PCAN_PARAMETER_OFF);
                    stsResult = PCANBasic.SetValue(m_PcanHandle, TPCANParameter.PCAN_BUSOFF_AUTORESET, ref iBuffer, sizeof(UInt32));
                    if (stsResult == TPCANStatus.PCAN_ERROR_OK)
                        IncludeTextMessage(string.Format("The automatic-reset on BUS-OFF was successfully {0}", bActivate ? "activated" : "deactivated"));
                    break;
                // The CAN option "Listen Only" will be set
                //
                case 3:
                    iBuffer = (uint)(bActivate ? PCANBasic.PCAN_PARAMETER_ON : PCANBasic.PCAN_PARAMETER_OFF);
                    stsResult = PCANBasic.SetValue(m_PcanHandle, TPCANParameter.PCAN_LISTEN_ONLY, ref iBuffer, sizeof(UInt32));
                    if (stsResult == TPCANStatus.PCAN_ERROR_OK)
                        IncludeTextMessage(string.Format("The CAN option \"Listen Only\" was successfully {0}", bActivate ? "activated" : "deactivated"));
                    break;
                // The feature for logging debug-information will be set
                //
                case 4:
                    iBuffer = (uint)(bActivate ? PCANBasic.PCAN_PARAMETER_ON : PCANBasic.PCAN_PARAMETER_OFF);
                    stsResult = PCANBasic.SetValue(PCANBasic.PCAN_NONEBUS, TPCANParameter.PCAN_LOG_STATUS, ref iBuffer, sizeof(UInt32));
                    if (stsResult == TPCANStatus.PCAN_ERROR_OK)
                        IncludeTextMessage(string.Format("The feature for logging debug information was successfully {0}", bActivate ? "activated" : "deactivated"));
                    break;
                // The channel option "Receive Status" will be set
                //
                case 5:
                    iBuffer = (uint)(bActivate ? PCANBasic.PCAN_PARAMETER_ON : PCANBasic.PCAN_PARAMETER_OFF);
                    stsResult = PCANBasic.SetValue(m_PcanHandle, TPCANParameter.PCAN_RECEIVE_STATUS, ref iBuffer, sizeof(UInt32));
                    if (stsResult == TPCANStatus.PCAN_ERROR_OK)
                        IncludeTextMessage(string.Format("The channel option \"Receive Status\" was set to {0}", bActivate ? "ON" : "OFF"));
                    break;
                // The feature for tracing will be set
                //
                case 7:
                    iBuffer = (uint)(bActivate ? PCANBasic.PCAN_PARAMETER_ON : PCANBasic.PCAN_PARAMETER_OFF);
                    stsResult = PCANBasic.SetValue(m_PcanHandle, TPCANParameter.PCAN_TRACE_STATUS, ref iBuffer, sizeof(UInt32));
                    if (stsResult == TPCANStatus.PCAN_ERROR_OK)
                        IncludeTextMessage(string.Format("The feature for tracing data was successfully {0}", bActivate ? "activated" : "deactivated"));
                    break;

                // The feature for identifying an USB Channel will be set
                //
                case 8:
                    iBuffer = (uint)(bActivate ? PCANBasic.PCAN_PARAMETER_ON : PCANBasic.PCAN_PARAMETER_OFF);
                    stsResult = PCANBasic.SetValue(m_PcanHandle, TPCANParameter.PCAN_CHANNEL_IDENTIFYING, ref iBuffer, sizeof(UInt32));
                    if (stsResult == TPCANStatus.PCAN_ERROR_OK)
                        IncludeTextMessage(string.Format("The procedure for channel identification was successfully {0}", bActivate ? "activated" : "deactivated"));
                    break;

                // The feature for using an already configured speed will be set
                //
                case 10:
                    iBuffer = (uint)(bActivate ? PCANBasic.PCAN_PARAMETER_ON : PCANBasic.PCAN_PARAMETER_OFF);
                    stsResult = PCANBasic.SetValue(m_PcanHandle, TPCANParameter.PCAN_BITRATE_ADAPTING, ref iBuffer, sizeof(UInt32));
                    if (stsResult == TPCANStatus.PCAN_ERROR_OK)
                        IncludeTextMessage(string.Format("The feature for bit rate adaptation was successfully {0}", bActivate ? "activated" : "deactivated"));
                    break;

                // The current parameter is invalid
                //
                default:
                    stsResult = TPCANStatus.PCAN_ERROR_UNKNOWN;
                    MessageBox.Show("Wrong parameter code.");
                    return;
            }

            // If the function fail, an error message is shown
            //
            if (stsResult != TPCANStatus.PCAN_ERROR_OK)
                MessageBox.Show(GetFormatedError(stsResult));
        }

        private void btnParameterGet_Click(object sender, EventArgs e)
        {
            TPCANStatus stsResult;
            UInt32 iBuffer;
            StringBuilder strBuffer;

            strBuffer = new StringBuilder(255);

            // Gets a PCAN-Basic parameter value
            //
            switch (cbbParameter.SelectedIndex)
            {
                // The Device-Number of an USB channel will be retrieved
                //
                case 0:
                    stsResult = PCANBasic.GetValue(m_PcanHandle, TPCANParameter.PCAN_DEVICE_NUMBER, out iBuffer, sizeof(UInt32));
                    if (stsResult == TPCANStatus.PCAN_ERROR_OK)
                        IncludeTextMessage(string.Format("The configured Device-Number is {0:X}h", iBuffer));
                    break;
                // The activation status of the 5 Volt Power feature of a PC-card or USB will be retrieved
                //
                case 1:
                    stsResult = PCANBasic.GetValue(m_PcanHandle, TPCANParameter.PCAN_5VOLTS_POWER, out iBuffer, sizeof(UInt32));
                    if (stsResult == TPCANStatus.PCAN_ERROR_OK)
                        IncludeTextMessage(string.Format("The 5-Volt Power of the USB/PC-Card is {0}", (iBuffer == PCANBasic.PCAN_PARAMETER_ON) ? "ON" : "OFF"));
                    break;
                // The activation status of the feature for automatic reset on BUS-OFF will be retrieved
                //
                case 2:
                    stsResult = PCANBasic.GetValue(m_PcanHandle, TPCANParameter.PCAN_BUSOFF_AUTORESET, out iBuffer, sizeof(UInt32));
                    if (stsResult == TPCANStatus.PCAN_ERROR_OK)
                        IncludeTextMessage(string.Format("The automatic-reset on BUS-OFF is {0}", (iBuffer == PCANBasic.PCAN_PARAMETER_ON) ? "ON" : "OFF"));
                    break;
                // The activation status of the CAN option "Listen Only" will be retrieved
                //
                case 3:
                    stsResult = PCANBasic.GetValue(m_PcanHandle, TPCANParameter.PCAN_LISTEN_ONLY, out iBuffer, sizeof(UInt32));
                    if (stsResult == TPCANStatus.PCAN_ERROR_OK)
                        IncludeTextMessage(string.Format("The CAN option \"Listen Only\" is {0}", (iBuffer == PCANBasic.PCAN_PARAMETER_ON) ? "ON" : "OFF"));
                    break;
                // The activation status for the feature for logging debug-information will be retrieved
                case 4:
                    stsResult = PCANBasic.GetValue(PCANBasic.PCAN_NONEBUS, TPCANParameter.PCAN_LOG_STATUS, out iBuffer, sizeof(UInt32));
                    if (stsResult == TPCANStatus.PCAN_ERROR_OK)
                        IncludeTextMessage(string.Format("The feature for logging debug information is {0}", (iBuffer == PCANBasic.PCAN_PARAMETER_ON) ? "ON" : "OFF"));
                    break;
                // The activation status of the channel option "Receive Status"  will be retrieved
                //
                case 5:
                    stsResult = PCANBasic.GetValue(m_PcanHandle, TPCANParameter.PCAN_RECEIVE_STATUS, out iBuffer, sizeof(UInt32));
                    if (stsResult == TPCANStatus.PCAN_ERROR_OK)
                        IncludeTextMessage(string.Format("The channel option \"Receive Status\" is {0}", (iBuffer == PCANBasic.PCAN_PARAMETER_ON) ? "ON" : "OFF"));
                    break;
                // The Number of the CAN-Controller used by a PCAN-Channel
                // 
                case 6:
                    stsResult = PCANBasic.GetValue(m_PcanHandle, TPCANParameter.PCAN_CONTROLLER_NUMBER, out iBuffer, sizeof(UInt32));
                    if (stsResult == TPCANStatus.PCAN_ERROR_OK)
                        IncludeTextMessage(string.Format("The CAN Controller number is {0}", iBuffer));
                    break;
                // The activation status for the feature for tracing data will be retrieved
                //
                case 7:
                    stsResult = PCANBasic.GetValue(m_PcanHandle, TPCANParameter.PCAN_TRACE_STATUS, out iBuffer, sizeof(UInt32));
                    if (stsResult == TPCANStatus.PCAN_ERROR_OK)
                        IncludeTextMessage(string.Format("The feature for tracing data is {0}", (iBuffer == PCANBasic.PCAN_PARAMETER_ON) ? "ON" : "OFF"));
                    break;
                // The activation status of the Channel Identifying procedure will be retrieved
                //
                case 8:
                    stsResult = PCANBasic.GetValue(m_PcanHandle, TPCANParameter.PCAN_CHANNEL_IDENTIFYING, out iBuffer, sizeof(UInt32));
                    if (stsResult == TPCANStatus.PCAN_ERROR_OK)
                        IncludeTextMessage(string.Format("The identification procedure of the selected channel is {0}", (iBuffer == PCANBasic.PCAN_PARAMETER_ON) ? "ON" : "OFF"));
                    break;
                // The extra capabilities of a hardware will asked
                //
                case 9:
                    stsResult = PCANBasic.GetValue(m_PcanHandle, TPCANParameter.PCAN_CHANNEL_FEATURES, out iBuffer, sizeof(UInt32));
                    if (stsResult == TPCANStatus.PCAN_ERROR_OK)
                        IncludeTextMessage(string.Format("The channel {0} Flexible Data-Rate (CAN-FD)", ((iBuffer & PCANBasic.FEATURE_FD_CAPABLE) == PCANBasic.FEATURE_FD_CAPABLE) ? "does support" : "DOESN'T SUPPORT"));
                    break;
                // The status of the speed adapting feature will be retrieved
                //
                case 10:
                    stsResult = PCANBasic.GetValue(m_PcanHandle, TPCANParameter.PCAN_BITRATE_ADAPTING, out iBuffer, sizeof(UInt32));
                    if (stsResult == TPCANStatus.PCAN_ERROR_OK)
                        IncludeTextMessage(string.Format("The feature for bit rate adaptation is {0}", (iBuffer == PCANBasic.PCAN_PARAMETER_ON) ? "ON" : "OFF"));
                    break;
                // The bitrate of the connected channel will be retrieved (BTR0-BTR1 value)
                //
                case 11:
                    stsResult = PCANBasic.GetValue(m_PcanHandle, TPCANParameter.PCAN_BITRATE_INFO, out iBuffer, sizeof(UInt32));
                    if (stsResult == TPCANStatus.PCAN_ERROR_OK)
                        IncludeTextMessage(string.Format("The bit rate of the channel is {0:X4}h", iBuffer));
                    break;
                // The bitrate of the connected FD channel will be retrieved (String value)
                //
                case 12:
                    stsResult = PCANBasic.GetValue(m_PcanHandle, TPCANParameter.PCAN_BITRATE_INFO_FD, strBuffer, 255);
                    if (stsResult == TPCANStatus.PCAN_ERROR_OK)
                        IncludeTextMessage(string.Format("The bit rate of the channel is {0}", strBuffer.ToString()));
                    break;
                // The nominal speed configured on the CAN bus
                //
                case 13:
                    stsResult = PCANBasic.GetValue(m_PcanHandle, TPCANParameter.PCAN_BUSSPEED_NOMINAL, out iBuffer, sizeof(UInt32));
                    if (stsResult == TPCANStatus.PCAN_ERROR_OK)
                        IncludeTextMessage(string.Format("The nominal speed of the channel is {0} bit/s", iBuffer));
                    break;
                // The data speed configured on the CAN bus
                //
                case 14:
                    stsResult = PCANBasic.GetValue(m_PcanHandle, TPCANParameter.PCAN_BUSSPEED_DATA, out iBuffer, sizeof(UInt32));
                    if (stsResult == TPCANStatus.PCAN_ERROR_OK)
                        IncludeTextMessage(string.Format("The data speed of the channel is {0} bit/s", iBuffer));
                    break;
                // The IP address of a LAN channel as string, in IPv4 format
                //
                case 15:
                    stsResult = PCANBasic.GetValue(m_PcanHandle, TPCANParameter.PCAN_IP_ADDRESS, strBuffer, 255);
                    if (stsResult == TPCANStatus.PCAN_ERROR_OK)
                        IncludeTextMessage(string.Format("The IP address of the channel is {0}", strBuffer.ToString()));
                    break;
                // The current parameter is invalid
                //
                default:
                    stsResult = TPCANStatus.PCAN_ERROR_UNKNOWN;
                    MessageBox.Show("Wrong parameter code.");
                    return;
            }

            // If the function fail, an error message is shown
            //
            if (stsResult != TPCANStatus.PCAN_ERROR_OK)
                MessageBox.Show(GetFormatedError(stsResult));
        }

        private void btnRead_Click(object sender, EventArgs e)
        {
            //TPCANStatus stsResult;

            // We execute the "Read" function of the PCANBasic                
            //
            //stsResult = m_IsFD ? ReadMessageFD() : ReadMessage();
            //if (stsResult != TPCANStatus.PCAN_ERROR_OK)
            //    // If an error occurred, an information message is included
            //    //
            //    IncludeTextMessage(GetFormatedError(stsResult));
        }

        private void btnGetVersions_Click(object sender, EventArgs e)
        {
            TPCANStatus stsResult;
            StringBuilder strTemp;
            string[] strArrayVersion;

            strTemp = new StringBuilder(256);

            // We get the vesion of the PCAN-Basic API
            //
            stsResult = PCANBasic.GetValue(PCANBasic.PCAN_NONEBUS, TPCANParameter.PCAN_API_VERSION, strTemp, 256);
            if (stsResult == TPCANStatus.PCAN_ERROR_OK)
            {
                IncludeTextMessage("API Version: " + strTemp.ToString());
                // We get the driver version of the channel being used
                //
                stsResult = PCANBasic.GetValue(m_PcanHandle, TPCANParameter.PCAN_CHANNEL_VERSION, strTemp, 256);
                if (stsResult == TPCANStatus.PCAN_ERROR_OK)
                {
                    // Because this information contains line control characters (several lines)
                    // we split this also in several entries in the Information List-Box
                    //
                    strArrayVersion = strTemp.ToString().Split(new char[] { '\n' });
                    IncludeTextMessage("Channel/Driver Version: ");
                    for (int i = 0; i < strArrayVersion.Length; i++)
                        IncludeTextMessage("     * " + strArrayVersion[i]);
                }
            }

            // If an error ccurred, a message is shown
            //
            if (stsResult != TPCANStatus.PCAN_ERROR_OK)
                MessageBox.Show(GetFormatedError(stsResult));
        }

        private void btnMsgClear_Click(object sender, EventArgs e)
        {
            // The information contained in the messages List-View
            // is cleared
            //
            //lock (m_LastMsgsList.SyncRoot)
            //{
            //    m_LastMsgsList.Clear();
            //    lstMessages.Items.Clear();
            //}
        }

        private void btnInfoClear_Click(object sender, EventArgs e)
        {
            // The information contained in the Information List-Box 
            // is cleared
            //
            lbxInfo.Items.Clear();
        }

        private TPCANStatus WriteFrame()
        {
            TPCANMsg CANMsg;
            TextBox txtbCurrentTextBox;

            // We create a TPCANMsg message structure 
            //
            CANMsg = new TPCANMsg();
            CANMsg.DATA = new byte[8];

            // We configurate the Message.  The ID,
            // Length of the Data, Message Type
            // and the data
            //
            CANMsg.ID = Convert.ToUInt32(txtID.Text, 16);
            CANMsg.LEN = Convert.ToByte(nudLength.Value);
            CANMsg.MSGTYPE = (chbExtended.Checked) ? TPCANMessageType.PCAN_MESSAGE_EXTENDED : TPCANMessageType.PCAN_MESSAGE_STANDARD;
            // If a remote frame will be sent, the data bytes are not important.
            //
            if (chbRemote.Checked)
                CANMsg.MSGTYPE |= TPCANMessageType.PCAN_MESSAGE_RTR;
            else
            {
                // We get so much data as the Len of the message
                //
                for (int i = 0; i < GetLengthFromDLC(CANMsg.LEN, true); i++)
                {
                    txtbCurrentTextBox = (TextBox)this.Controls.Find("txtData" + i.ToString(), true)[0];
                    CANMsg.DATA[i] = Convert.ToByte(txtbCurrentTextBox.Text, 16);
                }
            }

            // The message is sent to the configured hardware
            //
            return PCANBasic.Write(m_PcanHandle, ref CANMsg);
        }

        private TPCANStatus WriteFrameFD()
        {
            TPCANMsgFD CANMsg;
            TextBox txtbCurrentTextBox;
            int iLength;

            // We create a TPCANMsgFD message structure 
            //
            CANMsg = new TPCANMsgFD();
            CANMsg.DATA = new byte[64];

            // We configurate the Message.  The ID,
            // Length of the Data, Message Type 
            // and the data
            //
            CANMsg.ID = Convert.ToUInt32(txtID.Text, 16);
            CANMsg.DLC = Convert.ToByte(nudLength.Value);
            CANMsg.MSGTYPE = (chbExtended.Checked) ? TPCANMessageType.PCAN_MESSAGE_EXTENDED : TPCANMessageType.PCAN_MESSAGE_STANDARD;
            CANMsg.MSGTYPE |= (chbFD.Checked) ? TPCANMessageType.PCAN_MESSAGE_FD : TPCANMessageType.PCAN_MESSAGE_STANDARD;
            CANMsg.MSGTYPE |= (chbBRS.Checked) ? TPCANMessageType.PCAN_MESSAGE_BRS : TPCANMessageType.PCAN_MESSAGE_STANDARD;

            // If a remote frame will be sent, the data bytes are not important.
            //
            if (chbRemote.Checked)
                CANMsg.MSGTYPE |= TPCANMessageType.PCAN_MESSAGE_RTR;
            else
            {
                // We get so much data as the Len of the message
                //
                iLength = GetLengthFromDLC(CANMsg.DLC, (CANMsg.MSGTYPE & TPCANMessageType.PCAN_MESSAGE_FD) == 0);
                for (int i = 0; i < iLength; i++)
                {
                    txtbCurrentTextBox = (TextBox)this.Controls.Find("txtData" + i.ToString(), true)[0];
                    CANMsg.DATA[i] = Convert.ToByte(txtbCurrentTextBox.Text, 16);
                }
            }

            // The message is sent to the configured hardware
            //
            return PCANBasic.WriteFD(m_PcanHandle, ref CANMsg);
        }

        private void btnWrite_Click(object sender, EventArgs e)
        {
            TPCANStatus stsResult;

            // Send the message
            //
            stsResult = m_IsFD ? WriteFrameFD() : WriteFrame();

            // The message was successfully sent
            //
            if (stsResult == TPCANStatus.PCAN_ERROR_OK)
                IncludeTextMessage("Message was successfully SENT");
            // An error occurred.  We show the error.
            //			
            else
                MessageBox.Show(GetFormatedError(stsResult));
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            TPCANStatus stsResult;

            // Resets the receive and transmit queues of a PCAN Channel.
            //
            stsResult = PCANBasic.Reset(m_PcanHandle);

            // If it fails, a error message is shown
            //
            if (stsResult != TPCANStatus.PCAN_ERROR_OK)
                MessageBox.Show(GetFormatedError(stsResult));
            else
                IncludeTextMessage("Receive and transmit queues successfully reset");
        }

        private void btnStatus_Click(object sender, EventArgs e)
        {
            TPCANStatus stsResult;
            String errorName;

            // Gets the current BUS status of a PCAN Channel.
            //
            stsResult = PCANBasic.GetStatus(m_PcanHandle);

            // Switch On Error Name
            //
            switch (stsResult)
            {
                case TPCANStatus.PCAN_ERROR_INITIALIZE:
                    errorName = "PCAN_ERROR_INITIALIZE";
                    break;

                case TPCANStatus.PCAN_ERROR_BUSLIGHT:
                    errorName = "PCAN_ERROR_BUSLIGHT";
                    break;

                case TPCANStatus.PCAN_ERROR_BUSHEAVY: // TPCANStatus.PCAN_ERROR_BUSWARNING
                    errorName = m_IsFD ? "PCAN_ERROR_BUSWARNING" : "PCAN_ERROR_BUSHEAVY";
                    break;

                case TPCANStatus.PCAN_ERROR_BUSPASSIVE:
                    errorName = "PCAN_ERROR_BUSPASSIVE";
                    break;

                case TPCANStatus.PCAN_ERROR_BUSOFF:
                    errorName = "PCAN_ERROR_BUSOFF";
                    break;

                case TPCANStatus.PCAN_ERROR_OK:
                    errorName = "PCAN_ERROR_OK";
                    break;

                default:
                    errorName = "See Documentation";
                    break;
            }

            // Display Message
            //
            IncludeTextMessage(String.Format("Status: {0} ({1:X}h)", errorName, stsResult));
        }

        private void MainForm_SizeChanged(object sender, EventArgs e)
        {
            pLMain.Size = new Size(MainForm.GetInstance().Size.Width, MainForm.GetInstance().Size.Height - 85);
            pLContent.Size = new Size(MainForm.GetInstance().Size.Width, 35);
          
        }



        #endregion

        #region Timer event-handler

        private void tMMainFormRefresh_Tick(object sender, EventArgs e)
        {
            sBFeedbackShow(DateTime.Today.ToShortDateString() + " " + DateTime.Now.ToLongTimeString(), 3);
          
        }

        private void tMCheck_Tick(object sender, EventArgs e)
        {
            //检测总线上的模块是否存在 以优先级高的方式传送
            string message = "";
            string caption = "";
       
            if (!IsCheckConnection)
            {
                IsCheckConnection = true;
                IsDataRecieved = false;
                MessageProccessing.allID2.Clear();
                IDName = 0;
                pc.SearchModuleID();
            }
            else
            {
                IsCheckConnection = false;
                if (MessageProccessing.allID2.Count == 0 && !IsDataRecieved)
                {
                    //如果没收到节点ID信号，总线上也没收到任何数据
                    tMCheck.Enabled = false;
                    message = "您确定要关闭吗？";
                    caption = "总线上未检测到连接模块！";
                    MessageBoxButtons buttons = MessageBoxButtons.YesNo;
                    DialogResult result = MessageBox.Show(this, message, caption, buttons);
                    if (result == DialogResult.Yes)
                    {
                        this.Close();
                    }
                    else
                    {
                        return;
                    }
                }
                if (cBID.Text != "" && !MessageProccessing.allID2.Contains(Convert.ToInt16(cBID.Text)) && IDName != Convert.ToInt16(cBID.Text))  //此处判断如果前面为False,则不进行下一步判断
                {
                    tMCheck.Enabled = false;
                    MessageBox.Show("模块" + cBID.Text + "已断开连接！");
                }
            }
        

        }

        #endregion

        private void btnParameters_Click(object sender, EventArgs e)
        {
            pf = ParametersForm.GetInstance();
            pf.MdiParent = this;
            pf.Parent = pLMain;
            pf.BringToFront();
            pf.Show();
        }

        private void btnRun_Click(object sender, EventArgs e)
        {
            TestRun tr = TestRun.GetInstance();
            tr.MdiParent = this;
            tr.Parent = pLMain;
            tr.Location = new Point(410, 0);
            tr.BringToFront();
            tr.Show();
        }


        private void btnWave_Click(object sender, EventArgs e)
        {
            os = OscilloScope.GetInstance();
            os.MdiParent = this;
            os.Parent = pLMain;
            os.BringToFront();
            os.Show();
        }



        private void cBID_DropDown(object sender, EventArgs e)
        {
            MessageProccessing.allID.Clear();
            pc.SearchModuleID();
            //等待返回ID号
            Thread.Sleep(150);
            try
            {
                for (int i = 0; i < MessageProccessing.allID.Count; i++)
                {
                    if (!cBID.Items.Contains(MessageProccessing.allID[i]))
                    {
                        cBID.Items.Add(MessageProccessing.allID[i]);
                    }
                }

                if (MessageProccessing.allID.Count > 0)
                {
                    tMCheck.Enabled = true;
                }
            }
            catch (System.Exception ex)
            {
                MainForm.GetInstance().sBFeedbackShow(ex.Message, 1);
            }
           // short aa = Configuration.MemoryControlTable[5];
        }

        private void cBID_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cBID.Text != null)
            {
                btnParameters.Enabled = true;
                btnRun.Enabled = true;
                btnWave.Enabled = true;
                PCan.currentID = Convert.ToByte(cBID.Text);
                pc.WriteOneWord(Configuration.SYS_ID, PCan.currentID, PCan.currentID);
                InitialMemoryControlTable();
                if (pf != null)
                {
                    pf.RefreshlVParam(0);
                }
               
            }
            else
            {
                btnParameters.Enabled = false;
                btnRun.Enabled = false;
                btnWave.Enabled = false;
            }
        }

        public void sBFeedbackShow(string message, int index)
        {
            sBFeedback.Panels[index].Text = message;

        }

    }
}
