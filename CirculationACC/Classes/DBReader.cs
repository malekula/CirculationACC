﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Text.RegularExpressions;

namespace Circulation
{
    class DBReader : DB
    {
        public DBReader() { }

        public DataRow GetReaderByID(int ID)
        {
            DA.SelectCommand.CommandText = "select A.*,B.Photo fotka from Readers..Main A left join Readers..Photo B on A.NumberReader = B.IDReader where NumberReader = " + ID;
            DS = new DataSet();
            int i = DA.Fill(DS, "reader");
            if (i == 0) return null;
            return DS.Tables["reader"].Rows[0];
        }
        public bool Exists(int ID)
        {
            DA.SelectCommand.CommandText = "select 1 from Readers..Main where NumberReader = " + ID;
            DS = new DataSet();
            if (DA.Fill(DS, "reader") > 0) return true; else return false;
        }

        internal DataRow GetReaderByBAR(string BAR)
        {
            DA.SelectCommand.CommandText = "select top 1 A.*,B.Photo fotka from Readers..Main A left join Readers..Photo B on A.NumberReader = B.IDReader where BarCode = '" + BAR.Substring(1) + "'";
            DS = new DataSet();
            int i;
            try
            {
                i = DA.Fill(DS, "t");
            }
            catch
            {
                i = 0;
            }
            if (i > 0) return DS.Tables["t"].Rows[0];

            DA.SelectCommand.CommandText = "select top 1 A.*,B.Photo fotka from Readers..Main A left join Readers..Photo B on A.NumberReader = B.IDReader where NumberSC = '" + BAR.Substring(0, BAR.IndexOf(" ")) +
                                                                                       "' and SerialSC = '" + BAR.Substring(BAR.IndexOf(" ") + 1) + "'";
            DS = new DataSet();
            i = DA.Fill(DS, "t");
            if (i > 0) return DS.Tables["t"].Rows[0];
            return null;
        }

        internal bool IsAlreadyIssuedMoreThanFourBooks(ReaderVO r)
        {
            DA.SelectCommand.CommandText = "select * from Reservation_R..ISSUED_ACC where IDREADER = " + r.ID + " and IDSTATUS = 1";
            DS = new DataSet();
            int i = DA.Fill(DS, "t");
            if (i >= 4) return true; else return false;
        }

        internal DataTable GetFormular(int ID)
        {
            DA.SelectCommand.CommandText = "select ROW_NUMBER() over(order by A.DATE_ISSUE) num, " +
                                           " bar.SORT bar, " +
                                           " avtp.PLAIN avt, " +
                                           " titp.PLAIN tit, " +
                                           " cast(cast(A.DATE_ISSUE as varchar(11)) as datetime) iss, " +
                                           " cast(cast(A.DATE_RETURN as varchar(11)) as datetime) ret , A.ID idiss, A.IDREADER idr,E.PLAIN shifr " +
                                           "  from Reservation_R..ISSUED_ACC A " +
                                           " left join BJACC..DATAEXT tit on A.IDMAIN = tit.IDMAIN and tit.MNFIELD = 200 and tit.MSFIELD = '$a' " +
                                           " left join BJACC..DATAEXTPLAIN titp on tit.ID = titp.IDDATAEXT " +
                                           " left join BJACC..DATAEXT avt on A.IDMAIN = avt.IDMAIN and avt.MNFIELD = 700 and avt.MSFIELD = '$a' " +
                                           " left join BJACC..DATAEXTPLAIN avtp on avt.ID = avtp.IDDATAEXT " +
                                           " left join BJACC..DATAEXT bar on A.IDDATA = bar.IDDATA and bar.MNFIELD = 899 and bar.MSFIELD = '$w' " +
                                           " left join BJACC..DATAEXT EE on A.IDDATA = EE.IDDATA and EE.MNFIELD = 899 and EE.MSFIELD = '$j'" +
                                           " left join BJACC..DATAEXTPLAIN E on E.IDDATAEXT = EE.ID" +

                                           " where A.IDREADER = " + ID + " and A.IDSTATUS = 1";
            ;
            DS = new DataSet();
            int i = DA.Fill(DS, "formular");
            return DS.Tables["formular"];
        }

        internal void ProlongByIDISS(int IDISS, int days, int IDEMP)
        {
            DA.UpdateCommand.CommandText = "update Reservation_R..ISSUED_ACC set DATE_RETURN = dateadd(day," + days + ",DATE_RETURN) where ID = " + IDISS;
            DA.UpdateCommand.Connection.Open();
            DA.UpdateCommand.ExecuteNonQuery();
            DA.UpdateCommand.Connection.Close();

            DA.InsertCommand.Parameters.Clear();
            DA.InsertCommand.Parameters.Add("IDACTION", SqlDbType.Int);
            DA.InsertCommand.Parameters.Add("IDUSER", SqlDbType.Int);
            DA.InsertCommand.Parameters.Add("IDISSUED_ACC", SqlDbType.Int);
            DA.InsertCommand.Parameters.Add("DATEACTION", SqlDbType.DateTime);

            DA.InsertCommand.Parameters["IDACTION"].Value = 3;
            DA.InsertCommand.Parameters["IDUSER"].Value = IDEMP;
            DA.InsertCommand.Parameters["IDISSUED_ACC"].Value = IDISS;
            DA.InsertCommand.Parameters["DATEACTION"].Value = DateTime.Now;

            DA.InsertCommand.CommandText = "insert into Reservation_R..ISSUED_ACC_ACTIONS (IDACTION,IDEMP,IDISSUED_ACC,DATEACTION) values " +
                                            "(@IDACTION,@IDUSER,@IDISSUED_ACC,@DATEACTION)";
            DA.InsertCommand.Connection.Open();
            DA.InsertCommand.ExecuteNonQuery();
            DA.InsertCommand.Connection.Close();


        }

        internal DataTable GetReaderByFamily(string p)
        {
            DA.SelectCommand.CommandText = "select NumberReader, FamilyName, [Name], FatherName,DateBirth, RegistrationCity,RegistrationStreet, " +
                                           " Email from Readers..Main where lower(FamilyName) like lower('" + p + "')+'%'";
            DS = new DataSet();
            DA.Fill(DS, "t");
            return DS.Tables["t"];
        }

        internal void AddPhoto(byte[] fotka)
        {
            DA.UpdateCommand.CommandText = "insert into Readers..Photo (IDReader,Photo) values (189245,@fotka)";
            DA.UpdateCommand.Parameters.AddWithValue("fotka", fotka);
            DA.UpdateCommand.Connection.Open();
            DA.UpdateCommand.ExecuteNonQuery();
            DA.UpdateCommand.Connection.Close();
        }
        internal string GetEmail(ReaderVO reader)
        {
            DA.SelectCommand.CommandText = "select Email from Readers..Main where NumberReader = " + reader.ID;
            DataSet D = new DataSet();
            int i = DA.Fill(D);
            if (i == 0) return "";
            if (this.IsValidEmail(D.Tables[0].Rows[0][0].ToString()))
            {
                return D.Tables[0].Rows[0][0].ToString();
            }
            else
            {
                return "";
            }

        }
        public bool IsValidEmail(string strIn)
        {
            // Return true if strIn is in valid e-mail format.
            return Regex.IsMatch(strIn,
                   @"^(?("")("".+?""@)|(([0-9a-zA-Z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-zA-Z])@))" +
                   @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-zA-Z][-\w]*[0-9a-zA-Z]\.)+[a-zA-Z]{2,6}))$");
        }

        

        internal string GetLastDateEmail(ReaderVO reader)
        {
            DA.SelectCommand.CommandText = "select top 1 DATEACTION from Reservation_R..ISSUED_ACC_ACTIONS where IDACTION = 4 and IDISSUED_ACC = " + reader.ID+" order by DATEACTION desc";//когда актион равен 4 - это значит емаил отослали. только в этом случае 
                                                                                                                                                        //в поле IDISSUED_ACC хранится номер читателя а не номер выдачи
                                                                                                                                                         //это пипец конечно ну а чё?
            DataSet D = new DataSet();
            int i = DA.Fill(D,"t");
            if (i == 0) return "не отправлялось";
            return Convert.ToDateTime(D.Tables["t"].Rows[0][0]).ToString("dd.MM.yyyy HH:mm");
        }
    }
}