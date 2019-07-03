/*
 Decompiled from the existing dll and modified
 Author:-Vinay
 Date:-10-June-2019
 */
using Microsoft.MetadirectoryServices;
using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using utitity;

namespace BAT.Identity.HR
{
    public class MAExtensionObject : IMASynchronization
    {
        string logpath = string.Empty;
        string MIMServer = string.Empty;


        void IMASynchronization.Initialize()
        {

            utitity.xmlcommon xmpobj = new xmlcommon();
            string filePath = Utils.ExtensionsDirectory + "\\MIMConfig.xml";
            xmpobj.LoadXMLDoc(filePath);
            logpath = xmpobj.GetSingleXMLValue("Configurations/ErrorLog");
            logpath = logpath + DateTime.Now.ToString("dd-MM-yyyy") + ".log";
            MIMServer = xmpobj.GetSingleXMLValue("Configurations/MIMServer");

        }

        void IMASynchronization.Terminate()
        {
        }


        bool IMASynchronization.ShouldProjectToMV(CSEntry csentry, out string MVObjectType)
        {

            bool blnShouldProjectToMV = false;
            MVObjectType = null;
            switch (csentry.ObjectType)
            {

                case "Person":
                    try
                    {
                        if ((csentry["employeestatus"].IsPresent) && (csentry["employeestatus"].Value == "A"))
                        {
                            string strEnddate = string.Empty;
                            if (csentry["enddate"].IsPresent) { strEnddate = csentry["enddate"].Value.Substring(0, 10); }


                            if (csentry["provisioningType"].IsPresent)
                            {
                                if (csentry["provisioningType"].Value == "FNC")
                                {
                                    if (csentry["customDate2"].IsPresent) { strEnddate = csentry["customDate2"].Value.Substring(0, 10); }

                                    if (DateTime.Compare(Priordate(csentry["enddate"].Value.Substring(0, 10), csentry["customDate2"].Value.Substring(0, 10)).AddDays(1.0), DateTime.Now) > 0)
                                    {
                                        MVObjectType = "person";
                                        blnShouldProjectToMV = true;
                                    }
                                }
                                else// For Permanent If the end date is future dated
                                {
                                    if (strEnddate.Substring(0, 4) != "9999")
                                    {
                                        if (DateTime.Compare(Convert.ToDateTime(strEnddate).AddDays(1.0), DateTime.Now) > 0)
                                        {
                                            MVObjectType = "person";
                                            blnShouldProjectToMV = true;
                                        }
                                    }
                                    else if (strEnddate.Substring(0, 4) == "9999")
                                    {
                                        MVObjectType = "person";
                                        blnShouldProjectToMV = true;
                                    }
                                }
                            }

                        }
                        return blnShouldProjectToMV;

                    }
                    catch (Exception ex)
                    {
                        throw new EntryPointNotImplementedException(ex.Message);
                    }
                default: throw new UnexpectedDataException("Unexpected ObjectType = " + csentry.GetType());
            }
        }


        DeprovisionAction IMASynchronization.Deprovision(
          CSEntry csentry)
        {
            throw new EntryPointNotImplementedException();
        }

        bool IMASynchronization.FilterForDisconnection(CSEntry csentry)
        {
            throw new EntryPointNotImplementedException();
        }

        void IMASynchronization.MapAttributesForJoin(
          string FlowRuleName,
          CSEntry csentry,
          ref ValueCollection values)
        {
            throw new EntryPointNotImplementedException();
        }

        bool IMASynchronization.ResolveJoinSearch(
          string joinCriteriaName,
          CSEntry csentry,
          MVEntry[] rgmventry,
          out int imventry,
          ref string MVObjectType)
        {
            throw new EntryPointNotImplementedException();
        }

        void IMASynchronization.MapAttributesForImport(
          string FlowRuleName,
          CSEntry csentry,
          MVEntry mventry)
        {


            switch (FlowRuleName)
            {

                case "employeestatus":

                    //If employee status is not present dont update the employee status . 
                    if (!csentry["employeestatus"].IsPresent) break;
                    //Update the Employee status with A when it comes as Hire or New Hire Scenario
                    else
                    {
                        if (csentry["event_externalCode"].Value == "H" || csentry["event_externalCode"].Value == "R") { mventry["employeestatus"].Value = "A"; break; }
                        if (csentry["employeestatus"].Value == "U" || csentry["employeestatus"].Value == "P") { mventry["employeestatus"].Value = "A"; break; }
                    }
                    break;

                /* Flows _newSite and the _newSiteStartDate are meant only for the Movers scenarios. */
                case "_newSite":

                    /*update new site code if the effectve date is current or back dated*/
                    //Consider the site date update only for the IA or the International transfer events
                    if (csentry["eventReason"].IsPresent && (csentry["eventReason"].Value == "REHINT" || csentry["eventReason"].Value == "ADD_GA"))
                    {
                        if ((csentry["empJobEffectiveDate"].IsPresent) && (csentry["site"].IsPresent))
                        {
                            DateTime dtmEffcive_dt = new DateTime();
                            dtmEffcive_dt = Convert.ToDateTime(csentry["empJobEffectiveDate"].Value.Substring(0, 10));
                            dtmEffcive_dt = dtmEffcive_dt.AddHours(GetSiteTimeZone(csentry["site"].ToString()));
                            //if the metaverse _newSiteStartDate is a future date already then skip the update
                            if ((mventry["_newSiteStartDate"].IsPresent) && (DateTime.Compare(dtmEffcive_dt, Convert.ToDateTime(mventry["_newSiteStartDate"].Value)) < 0)) break;
                            //Otherwise if the ffective date is past dated then perform the following updates
                            if (DateTime.Compare(dtmEffcive_dt, DateTime.Now) > 0)
                            {
                                mventry["_newSite"].Value = csentry["site"].Value;
                                break;
                            }
                            if (mventry["_newSite"].IsPresent) { mventry["_newSite"].Delete(); break; }
                        }
                        if (mventry["_newSite"].IsPresent)
                            mventry["_newSite"].Delete();
                        break;
                    }
                    if (mventry["_newSite"].IsPresent)
                        mventry["_newSite"].Delete();
                    break;

                case "_newSiteStartDate":

                    /*update new site code only if the Mover's event is obtained effectve date is current/ back dated*/
                    //Consider the site date update only for the IA or the International transfer events
                    if (csentry["eventReason"].IsPresent && csentry["eventReason"].Value == "REHINT" && csentry["eventReason"].Value == "ADD_GA")
                    {
                        DateTime dtmEffective_dt = new DateTime();
                        if ((csentry["empJobEffectiveDate"].IsPresent) && (csentry["site"].IsPresent))
                        {
                            //Get the time zone as per the site
                            dtmEffective_dt = Convert.ToDateTime(csentry["empJobEffectiveDate"].Value.Substring(0, 10));
                            dtmEffective_dt = dtmEffective_dt.AddHours(GetSiteTimeZone(csentry["site"].ToString()));
                            //if the coming effective date is smaller than the already present newsite start date then skip the update
                            if ((mventry["_newSiteStartDate"].IsPresent) && (DateTime.Compare(dtmEffective_dt, Convert.ToDateTime(mventry["_newSiteStartDate"].Value)) < 0)) break;
                            //Update the new site date if the effective date is current/back dated 
                            if (DateTime.Compare(dtmEffective_dt, DateTime.Now) >= 0)
                            {
                                mventry["_newSiteStartDate"].Value = dtmEffective_dt.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.000'");
                                break;
                            }

                        }//Delete the existing effcetive date if the effcetive date is not popping up through success factor
                        else
                        {
                            mventry["_newSiteStartDate"].Delete();
                            break;
                        }

                    }
                    mventry["_newSiteStartDate"].Delete();
                    break;

                case "employeeEndDate":
                    try
                    {
                        //All Terminated/Retrired cases except the Movers (TERVASGN)--Skip the end date update if the observed event is Movers (TERVASGN)
                        if (csentry["eventReason"].Value == "TERVASGN") { break; }

                        //Handling the Terminated Events--Most Precedent Event for termination
                        if (csentry["event_externalCode"].IsPresent && csentry["event_externalCode"].Value == "26")
                        {
                            DateTime dtEffectiveenddate = new DateTime();
                            //Pick the next day of the given end date if its not the one with 9999 year
                            if (csentry["empJobEffectiveDate"].IsPresent)
                            {
                                dtEffectiveenddate = Convert.ToDateTime(csentry["empJobEffectiveDate"].Value.Substring(0, 10)).AddDays(1);
                                mventry["employeeEndDate"].Value = dtEffectiveenddate.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.000'");
                                break;
                            }
                        }
                        // Check for the employee type and accordingly update the end date
                        DateTime dtmenddate = new DateTime();

                        if (csentry["provisioningType"].IsPresent)
                        {
                            if (csentry["provisioningType"].Value == "FNC")
                            {
                                //If customdate2 (contract end date is not coming from SF) skip the update and log if it is a new hire that user doesnt have the end date 
                                if (!csentry["customDate2"].IsPresent) { break; }

                                //Pick the next day of the given contractor end date
                                dtmenddate = Convert.ToDateTime(csentry["customDate2"].Value.Substring(0, 10)).AddDays(1);
                            }
                            else//other than contractors (emp type=P||F)
                            {
                                if (mventry["employeeEndDate"].IsPresent) { break; }
                                if (!mventry["employeeEndDate"].IsPresent)
                                {
                                    dtmenddate = Convert.ToDateTime("9999-12-31");
                                }

                            }
                            mventry["employeeEndDate"].Value = dtmenddate.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.000'");
                        }
                        break;
                    }
                    catch (Exception ex)
                    {
                        File.AppendAllText(logpath, ex.Message + "\r\n");
                        break;
                    }

                case "_SFPreferredFName":
                    if (csentry["preferredFirstName"].IsPresent)
                    {
                        mventry["_SFPreferredFName"].Value = csentry["preferredFirstName"].Value;
                        break;
                    }
                    mventry["_SFPreferredFName"].Value = String.Empty;
                    break;

                case "payGrade":
                    if (csentry["payGrade"].IsPresent)
                    {
                        int intlength = csentry["payGrade"].Value.Length;
                        mventry["PayGrade"].Value = csentry["payGrade"].Value.Substring(2, intlength - 2);
                        break;
                    }
                    break;

                default:
                    throw new EntryPointNotImplementedException();
            }
        }

        void IMASynchronization.MapAttributesForExport(
          string FlowRuleName,
          MVEntry mventry,
          CSEntry csentry)
        {
            throw new EntryPointNotImplementedException();
        }


        //The function is used to get the exact time zone of the given site
        private double GetSiteTimeZone(string sitetimezone)
        {
            double num = 0.0;
            using (SqlConnection connection = new SqlConnection())
            {
                connection.ConnectionString = "Server=" + MIMServer + ";Database=FIMService;Trusted_Connection=true";
                connection.Open();
                using (SqlDataReader sqlDataReader = new SqlCommand("SELECT ValueText  \r\n                                                  FROM [FIMService].[fim].[ObjectValueText]\r\n                                                  where [ObjectKey] IN (SELECT [ObjectKey]      \r\n                                                  FROM [FIMService].[fim].[ObjectValueString]\r\n                                                  where ValueString LIKE '" + sitetimezone + "' AND AttributeKey IN(SELECT  [Key]     \r\n                                                  FROM [FIMService].[fim].[AttributeInternal]\r\n                                                  where NAME='BAT_CODE'))\r\n                                                  AND AttributeKey IN (SELECT  [Key]     \r\n                                                  FROM [FIMService].[fim].[AttributeInternal]\r\n                                                  where NAME='BAT_TIMEZONE')", connection).ExecuteReader())
                {
                    while (sqlDataReader.Read())
                        num = -Convert.ToDouble(sqlDataReader[0]);
                }
            }
            return num;
        }

        private DateTime Priordate(string strdatearg1, string strdatearg2)
        {
            //Compare the strdatearg1 and strdatearg2 and return the prior date
            if (DateTime.Compare(Convert.ToDateTime(strdatearg1), Convert.ToDateTime(strdatearg2)) < 0)
                return Convert.ToDateTime(strdatearg1);
            return Convert.ToDateTime(strdatearg2);
        }


    }
}
