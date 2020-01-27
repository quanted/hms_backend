﻿using System;
using AQUATOX.AQTSegment;
using AQUATOX.AQSite;
using AQUATOX.OrgMatter;
using AQUATOX.Nutrients;
using Newtonsoft.Json;
using Globals;

namespace AQUATOX.Chemicals
{

    public class ChemicalRecord
    {
        public string ChemName;
        public string CASRegNo;
        public double MolWt;
        //public double Solubility;
        //public string XSolubility;
        public double Henry;
        public string XHenry;
        public double pka;
        public string Xpka;
        //public double VPress;
        //public string XVPress;
        public double LogKow;    // Log KOW, log octanol water part. coeff.
        public string XLogKow;
        public double En;        // Activation Energy for Temperature
        public string XEn;
        public double KMDegrdn;
        public string XKMDegrdn;
        public double KMDegrAnaerobic;
        public string XKMDegrAnaerobic;
        public double KUnCat;
        public string XKUncat;
        public double KAcid;
        public string XKAcid;
        public double KBase;
        public string XKBase;
        public double PhotolysisRate;
        public string XPhotoLysisRate;
        //public double OxRateConst;
        //public string XOxRateConst;
        public double KPSed;
        public string XKPSed;
        //public double Weibull_Shape;
        //public string XWeibull_Shape;
        public bool ChemIsBase;
        public bool CalcKPSed;
        //public double CohesivesK1;
        //public double CohesivesK2;
        //public double CohesivesKp;
        //public string CohesivesRef;
        //public double NonCohK1;
        //public double NonCohK2;
        //public double NonCohKp;
        //public string NonCohRef;
        //public double NonCoh2K1;
        //public double NonCoh2K2;
        //public double NonCoh2Kp;
        //public string NonCoh2Ref;

        //// PFA Parameters
        public bool IsPFA;
        public string PFAType;
        //public double PFAChainLength;
        //public string XPFAChainLength;
        public double PFASedKom;
        public string XPFASedKom;
        //public double PFAAlgBCF;
        //public string XPFAAlgBCF;
        //public double PFAMacroBCF;
        //public string XPFAMacroBCF;
        //public double WeibullSlopeFactor;
        //public string XWeibullSlopeFactor;
        public bool CalcKOMRefrDOM;
        public double KOMRefrDOM;
        public string XKOMRefrDOM;
        public double K1Detritus;
        public string XK1Detritus;
        public bool BCFUptake;
    } // end ChemicalRecord




    public class TToxics : TStateVariable

    {
        [JsonIgnore] public double ppb = 0;
        public AllVariables Carrier = AllVariables.NullStateVar;
        public bool IsAGGR = false;
        public ChemicalRecord ChemRec;
        public double Tox_Air;  // toxicant in air (gas-phase concentration) in g/m3
        // [JsonIgnore] public double RecrSave = 0;  // recruitment for dothiseverystep.  (nosave)
        public Loadings.TLoadings GillUptake_Link = null;  // optional linkage from JSON if chemicals sorbed to plants, animals, or OM not modeled
        public Loadings.TLoadings Depuration_Link = null;
        public Loadings.TLoadings Sorption_Link = null;
        public Loadings.TLoadings Decomposition_Link = null;
        public Loadings.TLoadings Desorption_Link = null;
        public Loadings.TLoadings PlantUptake_Link = null;


        // -------------------------------------

        public TToxics(AllVariables Ns, AllVariables Carry, T_SVType SVT, T_SVLayer L, string aName, AQUATOXSegment P, double IC) : base(Ns, SVT, L, aName, P, IC)
        {
            Carrier = Carry;
            // if (Ns >= Consts.FirstOrgTox && Ns <= Consts.LastOrgTox)
            // {
            //     RequiresData = true;
            // }
            ppb = 0;
//          RecrSave = 0;
        }

        public static T_SVType AssocToxTyp(AllVariables S)
        {
            T_SVType result;   
            result = (T_SVType)((int)(S) + 2);
            return result;
        }


        // ==================================================
        // chemical and microbial degradation
        // partitioning among phases
        // ==================================================
        // (************************************)
        // (* temperature correction - chemical*)
        // (************************************)
        public double Arrhen(double Temperature)
        {
            double result;
            const double R = 1.987;
            // univ. gas in cal/deg mol
            const double Kelvin0 = 273.16;
            const double TObs = 25.0;
            double Intermed1;
            double Intermed2;
            // to use in exp
            if (ChemRec.En <= 0.0)
            {
                ChemRec.En = 18000.0;  // average value as default
                // cal/mol
            }
            Intermed1 = ChemRec.En / (R * (Temperature + Kelvin0));
            Intermed2 = ChemRec.En / (R * (TObs + Kelvin0));
            result = Math.Exp(-(Intermed1 - Intermed2));
            // with

            return result;
        }

        // arrhen
        // (*************************************)
        // (*           hydrolysis              *)
        // (*************************************)
        public double Hydrolysis()
        {
            double KAcidExp;
            double KHyd;
            double Hydrol;
            double KBaseExp;
            //double HydrolInKg;
            KAcidExp = ChemRec.KAcid * Math.Pow(10.0, -AQTSeg.GetState(AllVariables.pH, T_SVType.StV, T_SVLayer.WaterCol));       // acid catalyzed
            KBaseExp = ChemRec.KBase * Math.Pow(10.0, AQTSeg.GetState(AllVariables.pH, T_SVType.StV, T_SVLayer.WaterCol) - 14.0); // base catalyzed

            KHyd = (KAcidExp + KBaseExp + ChemRec.KUnCat) * Arrhen(AQTSeg.GetState(AllVariables.Temperature, T_SVType.StV, T_SVLayer.WaterCol));
            Hydrol = KHyd * State;
            return Hydrol;

            //ToxLossRecord _wvar3 = AQTSeg.ToxLossArray[OrgType()];
            //// save for tox loss output & categorization
            //HydrolInKg = Hydrol * AQTSeg.SegVol() * 1000.0 * 1e-9;
            //// kg       // ug/L           // m3   // L/m3    // kg/ug

            //if (NState >= AllVariables.POC_G1 && NState <= AllVariables.POC_G3)
            //    HydrolInKg = Hydrol * AQTSeg.Location.Locale.SurfArea * 1e-9;
            //    // kg     // ug/m2            // m2                 // kg/ug

            //TotalToxLoss[DerivStep] = TotalToxLoss[DerivStep] + HydrolInKg;
            //Hydrolys[DerivStep] = Hydrolys[DerivStep] + HydrolInKg;
            // tox dissolved in water

            //if ((SVType == T_SVType.StV) || (new ArrayList(new object[] { AllVariables.DissRefrDetr, AllVariables.DissLabDetr }).Contains(NState)))
            //{
            //    _wvar3.DissHydr[DerivStep] = _wvar3.DissHydr[DerivStep] + HydrolInKg;
            //}
            //if ((new ArrayList(new object[] { AllVariables.SedmRefrDetr, AllVariables.SedmLabDetr, AllVariables.POC_G1 }).Contains(NState)))
            //{
            //    _wvar3.SedHydr[DerivStep] = _wvar3.SedHydr[DerivStep] + HydrolInKg;
            //}
        }

        // hydrolysis
        // (************************************)
        // (*            oxidation             *)
        // (************************************)
        public double Oxidation()
        {
            double oxid;
            oxid = 0.0;
            // If Oxidation turned back on, enable mass balance tracking
            return oxid;
        }

        // (****************************)
        // (*  simple approach of      *)
        // (* Schwarzenbach et al. 1993 )
        // (****************************)
        public double Photolysis()
        {
            double result;
            double Solar0;
            double LightFactor;
            double ScreeningFactor;
            //  double PhotolInKg;
            double RadDistr0;
            double Thick;
            double KPhot;
            double PhotoL;
            double PhotRate;
            double Alpha;
            const int AveSolar = 500;
            // L/d
            const double RadDistr = 1.6;
            Alpha = AQTSeg.Extinct(false, true, true, false, 0);
            // don't include periphyton, but include all other plants

            if (ChemRec.PhotolysisRate > 0.0)
            {
            //    if (_wvar1.VSeg == VerticalSegments.Epilimnion)
            //    {

            Solar0 = AQTSeg.GetState(AllVariables.Light, T_SVType.StV, T_SVLayer.WaterCol);
            RadDistr0 = 1.2;

            //    }
            //    else
            //    {
            //        Solar0 = _wvar1.AQTSeg.GetState(AllVariables.Light, T_SVType.StV, T_SVLayer.WaterCol) * Math.Exp(-Alpha * _wvar1.Location.MeanThick[VerticalSegments.Epilimnion]);
            //        RadDistr0 = 1.6;
            //    }

             
            Thick = AQTSeg.Location.MeanThick;
            PhotRate = ChemRec.PhotolysisRate;
            LightFactor = Solar0 / AveSolar;
            ScreeningFactor = (RadDistr / RadDistr0) * ((1 - Math.Exp(-Alpha * Thick)) / (Alpha * Thick));
            KPhot = PhotRate * ScreeningFactor * LightFactor;
            PhotoL = KPhot * State;

            }
            else PhotoL = 0.0;  

            if (PhotoL < 0.0) {   PhotoL = 0.0;  }

            result = PhotoL;
            //TStates _wvar2 = AllStates;
            //ToxLossRecord _wvar3 = ToxLossArray[OrgType()];
            //// save for tox loss output & categorization
            //PhotolInKg = PhotoL * SegVol() * 1000.0 * 1e-9;
            //// kg       // ug/L           // m3    // L/m3 // kg/ug
            //_wvar3.Photolys[DerivStep] = _wvar3.Photolys[DerivStep] + PhotolInKg;
            //_wvar3.TotalToxLoss[DerivStep] = _wvar3.TotalToxLoss[DerivStep] + PhotolInKg;
            //if ((SVType == T_SVType.StV) || (new ArrayList(new object[] { AllVariables.DissRefrDetr, AllVariables.DissLabDetr }).Contains(NState)))
            //{
            //    _wvar3.DissPhot[DerivStep] = _wvar3.DissPhot[DerivStep] + PhotolInKg;
            //}

            return result;
        }

        // Photolysis
        // (********************************)
        // (*       Volatilization         *)
        // (*   based on formulations in   *)
        // (*   Schwarzenbach et al. 1993  *)
        // (********************************)
        public virtual double Volatilization()
        {
            double result;
            // Saturation concentration of pollutant mg/m3
            // mass transfer coeff., m/d
            // liquid-phase mass trans. coeff., m/d
            // gas-phase mass trans. coeff., m/d
            TStateVariable TWind;
            double WindLd;
            double Volat;
//            double VolatInKg;
            double ToxSat;
            double KOVol;
            double KLiq;
            double KGas;
            double Temp;     // degrees C at time of evaluation
            double T;        // degrees Kelvin
            TSalinity Sal;     // salinity o/oo
            double HLCSaltFactor;    // henry law salinity factor, unitless
            double HenryLaw;        // local, atm cu m/mol units -> unitless
            const double Gas = 8.206E-5;      // atm cu m/mol K    // R in tech doc.
            const double Kelvin0 = 273.16;
//            const double cmpsec2mpd = 864.0;

            result = 0;
            if ((State < Consts.Tiny))
            {
                return result;
            }
            Temp = AQTSeg.GetState(AllVariables.Temperature, T_SVType.StV, T_SVLayer.WaterCol);
            if ((ChemRec.Henry > 0.0) && (Temp > AQTSeg.Ice_Cover_Temp())  && (NonDissoc() > Consts.VSmall))  // && (AQTSeg.VSeg == VerticalSegments.Epilimnion)
            {
                T = Kelvin0 + AQTSeg.GetState(AllVariables.Temperature, T_SVType.StV, T_SVLayer.WaterCol);
                // to correct for T of obs.

                Sal = (TSalinity) AQTSeg.GetStatePointer(AllVariables.Salinity, T_SVType.StV, T_SVLayer.WaterCol);
                if (Sal != null) HLCSaltFactor = 1 + 0.01143 * Sal.State;
                else HLCSaltFactor = 1;

                HenryLaw = (ChemRec.Henry * HLCSaltFactor) / (Gas * T);
                // unitless =(atm cu m/mol)   unitless     / ((atm cu m/mol K) * K)

                TWind = AQTSeg.GetStatePointer(AllVariables.WindLoading, T_SVType.StV, T_SVLayer.WaterCol);
                if (TWind == null) WindLd = 0.1;
                else  WindLd = TWind.State * 0.5;
                // m/s at 10 m to m/s at 10 cm based on Banks, 1975
                if (WindLd < 0.05) WindLd = 0.05;

                KGas = 168.0 * WindLd * Math.Pow((18.0 / ChemRec.MolWt), 0.25);
                // Thomann & Muller,'87
                // m/d              m/s
                // KGas := (0.3 + 0.2 * WindLd) * POWER((18.0/MolWt), 1.17) * cmpsec2mpd * HenryLaw;
                // m/d                 m/s                                   m/d/(cm/s)   unitless

                if (NonDissoc() < Consts.Small)  KOVol = 0.0;
                else
                {
                    KLiq = ((AQTSeg.GetStatePointer(AllVariables.Oxygen, T_SVType.StV, T_SVLayer.WaterCol)) as TO2Obj).KReaer() * AQTSeg.Location.MeanThick * Math.Pow((32.0 / ChemRec.MolWt), 0.25) * 1.0 / NonDissoc();
                    // m/d                                          /d                        m           unitless      unitless fraction
                    if (KLiq == 0)
                    {   KOVol = 0; }
                    else
                    {   KOVol = 1.0 / (1.0 / KLiq + 1.0 / (KGas * HenryLaw * NonDissoc()));}
                    // sum of resistances
                    // m/d                           m/d         m/d    unitless
                }
                ToxSat = Tox_Air / (HenryLaw * NonDissoc()) * 1000;
     // mg/cu m or ug/L  // g/m3     // unitless    // unitless  // mg/g

                Volat = KOVol / Location.MeanThick * (ToxSat - State);
                // ug/L d    m/d            m          mg/cu m   mg/cu m
            }
            else
            {   Volat = 0.0;}

            result = -Volat;

            //if (Volat < 0)
            //{
            //    TStates _wvar3 = AllStates;
            //    ToxLossRecord _wvar4 = _wvar3.ToxLossArray[OrgType()];
            //    // save for tox loss output & categorization
            //    VolatInKg = -Volat * _wvar3.SegVol() * 1000.0 * 1e-9;
            //    TotalToxLoss[_wvar3.DerivStep] = TotalToxLoss[_wvar3.DerivStep] + VolatInKg;
            //    Volatiliz[_wvar3.DerivStep] = Volatiliz[_wvar3.DerivStep] + VolatInKg;
            //    DissVolat[_wvar3.DerivStep] = DissVolat[_wvar3.DerivStep] + VolatInKg;
            //}
            //// with

            return result;
        }

        // (**********************************************)
        // (*      microbial degradation of chemical     *)
        // (**********************************************)
        public double MicrobialMetabolism(ref double FracAerobic)
        {
            double KM;
            double Microbial;
            //double MicrobInKg;
            //double PWVolumeInL;
            //TPorewater PPore;
            FracAerobic = 0;
            if (ChemRec.KMDegrdn > 0.0)
            {
                KM = ChemRec.KMDegrdn;
                // KM usually is determined for water  12-10-1997
                
                if (NState != AllVariables.H2OTox)    // if the toxicant is not in the water phase
                {  KM = KM * 4.0;  }
                // increased activity in detritus as opposed to water, see Gunnison '85
                // based on AECos experiments, Athens EPA Lab

                Microbial = Decomposition(KM, ChemRec.KMDegrAnaerobic, ref FracAerobic);
            }
            else
            {   Microbial = 0.0;    }
  
            //// ----------------------write mass balance output ------------------------
            //ToxLossRecord _wvar3 = ToxLossArray[OrgType()];
            //// save for tox loss output & categorization
            //PPore = AQTSeg.GetStatePointer(AllVariables.PoreWater, T_SVType.StV, Layer);
            //PWVolumeInL = 0;
            //if (PPore != null)
            //{
            //    if ((PPore.VolumeInM3() > Consts.Tiny))
            //        PWVolumeInL = PPore.VolumeInL();
            //}
            //if (Layer == T_SVLayer.WaterCol)
            //{   // kg       // ug/L wc           // m3 wc   // L/m3  // kg/ug
            //    MicrobInKg = Microbial * SegVol() * 1000.0 * 1e-9;
            //}
            //else if ((NState >= AllVariables.PoreWater && NState <= AllVariables.LaDOMPore))
            //{   // kg        // ug/L pw     // L pw    // kg/ug
            //    MicrobInKg = Microbial * PWVolumeInL * 1e-9;
            //}
            //else if ((NState >= AllVariables.POC_G1 && NState <= AllVariables.POC_G3))
            //{   // kg      // ug/m2          // m2            // kg/ug
            //    MicrobInKg = Microbial * Location.Locale.SurfArea * 1e-9;
            //}
            //else
            //{   MicrobInKg = Microbial * SedLayerArea() * 1e-9;   }
            //// kg           // ug/m2          // m2      // kg/ug

            //TotalToxLoss[DerivStep] = TotalToxLoss[DerivStep] + MicrobInKg;
            //MicrobMet[DerivStep] = MicrobMet[DerivStep] + MicrobInKg;

            //if (Layer == T_SVLayer.WaterCol)
            //{
            //    // tox dissolved in water
            //    if ((SVType == T_SVType.StV) || (new ArrayList(new object[] { AllVariables.DissRefrDetr, AllVariables.DissLabDetr }).Contains(NState)))
            //    {   DissMicrob[DerivStep] = _wvar3.DissMicrob[DerivStep] + MicrobInKg;
            //    }
            //    if ((new ArrayList(new object[] { AllVariables.SedmRefrDetr, AllVariables.SedmLabDetr }).Contains(NState)))
            //    {   SedMicrob[DerivStep] = _wvar3.SedMicrob[DerivStep] + MicrobInKg;
            //    }
            //}
            //// ----------------------write mass balance output ------------------------

            return Microbial;
        }

        (************************************)
         (* biotransformation         *)
         (************************************)
        public double Biotransformation()
        {
            double result;
            TStateVariable Carry;
            double BioTInKg;
            result = 0;
            Carry = AQTSeg.GetStatePointer(NState, T_SVType.StV, T_SVLayer.WaterCol);
            if (Carry.IsAnimal())
            {
                result = State * ((Carry) as TAnimal).Anim_Tox[OrgType()].Bio_rate_const;
            }
            if (Carry.IsPlant())
            {
                result = State * ((Carry) as TPlant).Plant_Tox[OrgType()].Bio_rate_const;
            }
            TStates _wvar1 = AllStates;
            ToxLossRecord _wvar2 = _wvar1.ToxLossArray[OrgType()];
            // save for tox loss output & categorization
            BioTInKg = result * _wvar1.SegVol() * 1000.0 * 1e-9;
            // kg
            // ug/L
            // m3
            // L/m3
            // kg/ug
            TotalToxLoss[_wvar1.DerivStep] = TotalToxLoss[_wvar1.DerivStep] + BioTInKg;
            BioTransf[_wvar1.DerivStep] = BioTransf[_wvar1.DerivStep] + BioTInKg;
            return result;
        }

        (**************************************************************)
         (* Microbial_BioTrans_To_This_SV       JSC, 5/27/99    *)
         (**)
         (* Calculates the Microbial BioTransformation from other     *)
         (* org. chemicals into this H2OTox or Detrital Compartment   *)
         (**)
         (**************************************************************)
        public double Microbial_BioTrans_To_This_SV(bool Aerobic)
        {
            double result;
            double MBTSum;
            T_SVType ToxLoop;
            double FracToMe;
            TBioTransObject BTRec;
            double MicrobM;
            double FracAerobic;
            TToxics ToxPtr;
            if (!(new ArrayList(new object[] { AllVariables.PoreWater, AllVariables.ReDOMPore, AllVariables.LaDOMPore, Consts.FirstOrgTox, Consts.FirstDetr, AllVariables.Cohesives, AllVariables.POC_G1 }).Contains(NState)))
            {
                throw new Exception("Programming Error, BioTrans_To_This_SV must be passed an org tox, sed, or detrital category");
            }
            MBTSum = 0;
            for (ToxLoop = Consts.FirstOrgTxTyp; ToxLoop <= Consts.LastOrgTxTyp; ToxLoop++)
            {
                // loop through all org toxicants
                // Tox In Water
                if (NState >= Consts.FirstTox && NState <= Consts.LastTox)
                {
                    ToxPtr = AQTSeg.GetStatePointer(Consts.AssocToxSV(ToxLoop), T_SVType.StV, Layer);
                }
                else
                {
                    ToxPtr = AQTSeg.GetStatePointer(NState, ToxLoop, Layer);
                }
                if (ToxPtr != null)
                {
                    if ((ToxPtr.State > 0))
                    {
                        // if this toxicant exists then
                        TChemical ChemRec = AQTSeg.Chemptrs[ToxLoop];
                        if (Aerobic)
                        {
                            BTRec = ChemRec.Get_BioTrans_Record(BioTransType.BTAerobicMicrobial, AllVariables.NullStateVar);
                        }
                        else
                        {
                            BTRec = ChemRec.Get_BioTrans_Record(BioTransType.BTAnaerobicMicrobial, AllVariables.NullStateVar);
                        }
                        FracToMe = BTRec.Percent[OrgType()] / 100.0;
                        // fraction of biotrans to this org tox compartment
                        if (FracToMe > 0)
                        {
                            MicrobM = ToxPtr.MicrobialMetabolism(ref FracAerobic);
                            // also returns frac aerobic
                            if (Aerobic)
                            {
                                MicrobM = MicrobM * FracAerobic;
                            }
                            else
                            {
                                MicrobM = MicrobM * (1 - FracAerobic);
                            }
                            MBTSum = MBTSum + (MicrobM * FracToMe);
                        }
                    }
                }
            }
            result = MBTSum;
            return result;
        }

        (**************************************************************)
         (* Biotrans_To_This_Org      JSC, 5/27/99    *)
         (**)
         (* Calculates all of the BioTransformation from other*)
         (* organic chemicals into this organism toxicant compartment*)
         (**)
         (**************************************************************)
        public double Biotrans_To_This_Org()
        {
            double result;
            T_SVType ToxLoop;
            double BTSum;
            TBioTransObject BTRec;
            TAnimal AnimPtr;
            double FracToMe;
            TToxics AnimToxPtr;
            if (!(NState >= Consts.FirstBiota && NState <= Consts.LastBiota))
            {
                throw new Exception("Programming Error, BioTrans_To_This_Org must be passed an organism");
            }
            BTSum = 0;
            for (ToxLoop = Consts.FirstOrgTxTyp; ToxLoop <= Consts.LastOrgTxTyp; ToxLoop++)
            {
                // loop through all org toxicants
                // if this toxicant is relevant then
                if ((ToxLoop != SVType) && (AQTSeg.GetState(NState, ToxLoop, T_SVLayer.WaterCol) > Consts.Tiny))
                {
                    TChemical ChemRec = AQTSeg.Chemptrs[ToxLoop];
                    BTRec = ChemRec.Get_BioTrans_Record(BioTransType.BTUserSpecified, NState);
                    // see if species specific Biotrans data exists
                    if (BTRec == null)
                    {
                        switch (NState)
                        {
                            // otherwise, use general parameterization
                            // Modify the A .. B: Consts.FirstAlgae .. Consts.LastAlgae
                            case Consts.FirstAlgae:
                                BTRec = ChemRec.Get_BioTrans_Record(BioTransType.BTAlgae, AllVariables.NullStateVar);
                                break;
                            // Modify the A .. B: Consts.FirstFish .. Consts.LastFish
                            case Consts.FirstFish:
                                BTRec = ChemRec.Get_BioTrans_Record(BioTransType.BTFish, AllVariables.NullStateVar);
                                break;
                            default:
                                AnimPtr = AQTSeg.GetStatePointer(NState, T_SVType.StV, T_SVLayer.WaterCol);
                                if ((AnimPtr.PAnimalData.Animal_Type == "Benthic Insect"))
                                {
                                    BTRec = ChemRec.Get_BioTrans_Record(BioTransType.BTBenthInsect, AllVariables.NullStateVar);
                                }
                                else
                                {
                                    BTRec = ChemRec.Get_BioTrans_Record(BioTransType.BTOtherInvert, AllVariables.NullStateVar);
                                }
                                break;
                                // else
                        }
                    }
                    // Case
                    FracToMe = BTRec.Percent[OrgType()] / 100.0;
                    // fraction of biotrans to this org tox compartment
                    if (FracToMe > 0)
                    {
                        AnimToxPtr = AQTSeg.GetStatePointer(NState, ToxLoop, T_SVLayer.WaterCol);
                        BTSum = BTSum + (FracToMe * AnimToxPtr.Biotransformation());
                    }
                }
            }
            // loop through toxicants
            result = BTSum;
            return result;
        }

        // (************************************)
        // (*              ionization          *)
        // (************************************)
        public double Ionization()
        {
            return 0;

            // (*  Function BaseIonize : Double;
            // BEGIN
            // with ChemPtrs^.ChemRec do begin
            // BaseIonize := StVar[Phase] * POWER(10.0,-(pH-2.0))
            // /(POWER(10.0, -Pka) + POWER(10.0, -(pH-2.0)));
            // {pH at colloid surface = bulk pH -2}
            // end;
            // END;*)
            // (* baseionize *)
            // (******************************)
            // (* ionization of acid compound*)
            // (******************************)
            // (*    Function AcidIonize:  Double;
            // BEGIN
            // with ChemRec do begin
            // if pka <> 0.0 then
            // AcidIonize := StVar[Phase] * POWER(10.0, -Pka)
            // /(POWER(10.0, -Pka) + POWER(10.0,-pH))
            // else
            // AcidIonize := 0.0;
            // end;
            // END;*)
            // (*acidionize*)
            // Ionization
            // (*   with ChemRec do begin
            // If pka < 0.0 then
            // Ionize[Phase] := BaseIonize
            // else
            // Ionize[Phase] := AcidIonize;
            // Ionization := Ionize[Phase];
            // end; *)

        }

        //public void CalculateLoadByBCF()
        //{
        //    double Org_PPB;
        //    double BCF;
        //    double CarrierState;
        //    double ToxState;
        //    TOrganism PCarrier;
        //    PCarrier = AQTSeg.GetStatePointer(NState, T_SVType.StV, T_SVLayer.WaterCol);
        //    CarrierState = PCarrier.State;
        //    // mg dry/L
        //    ToxState = AQTSeg.GetState(Consts.AssocToxSV(SVType), T_SVType.StV, T_SVLayer.WaterCol);
        //    // ug/L
        //    if (NState >= Consts.FirstDetr && NState <= Consts.LastDetr)
        //        BCF = CalculateKOM();
        //    else
        //        BCF = PCarrier.BCF(AQTSeg.CalculateTElapsed(SVType), SVType);
        //    // L/kg dry

        //    Org_PPB = ToxState * BCF;
        //    // ug/kg dry // ug/L  // L/Kg dry
        //    State = Org_PPB * CarrierState / 1e6;
        //    // ug/L     ug/kg dry   mg dry /L    mg/kg

        //}

        // (*******************************)
        // (* obtain loading for unit vol *)
        // (*******************************)
        public override void CalculateLoad(DateTime TimeIndex)
        {
            // atmospheric and point-source loadings should be to epilimnion in single-segment mode. 11/19/96
            // All chemical loadings to upper layer for estuary version, biota split between layers 10-17-02
            double SegVolume;
            double Inflow;
            int Loop;
            //TStateVariable CPtr;
            //double CarrierLdg;
            double AddLoad;
            //double LoadRes;
            //DetritalInputRecordType PInputRec;
            //double ToxLoad;
            //double Wet2Dry;

            Loading = 0;

            //if (ChemRec.BCFUptake && (new ArrayList(new object[] { Consts.FirstDetr, Consts.FirstBiota }).Contains(NState)))
            //{
            //    CalculateLoadByBCF();
            //    return;
            //}

            SegVolume = AQTSeg.SegVol();
            //if ((SegVolume == 0))  {  throw new Exception("Water Volume is Zero, Cannot continue Simulation");    }

            Inflow = Location.Morph.InflowH2O;  //  * _wvar1.OOSInflowFrac;

            if (new ArrayList(new object[] { AllVariables.DissRefrDetr, AllVariables.Cohesives }).Contains(NState))
            {
                // Code to support organisms or sediments with PS, NPS, Inflow Loadings
                CPtr = AQTSeg.GetStatePointer(Carrier, T_SVType.StV, T_SVLayer.WaterCol);
                Loading = 0;
                if (NState >= AllVariables.DissRefrDetr && NState <= AllVariables.SuspLabDetr)
                {
                    // Split into four compartments
                    PInputRec = ((AQTSeg.GetStatePointer(AllVariables.DissRefrDetr, T_SVType.StV, T_SVLayer.WaterCol)) as TDissRefrDetr).InputRecord;
                    LoadRes = Loadings.Units.Loadings.ReturnLoad(TimeIndex, PInputRec.Load) * ((CPtr) as TDetritus).MultFrac(TimeIndex, false, null);
                    ToxLoad = Loadings.Units.Loadings.ReturnLoad(TimeIndex, PInputRec.ToxLoad[SVType]);
                }
                else
                {
                    LoadRes = Loadings.Units.Loadings.ReturnLoad(TimeIndex, CPtr.LoadsRec);
                    ToxLoad = Loadings.Units.Loadings.ReturnLoad(TimeIndex, LoadsRec);
                }
                LoadRes = LoadRes * Inflow / SegVolume * ToxLoad / 1e6;
                // ug/L       mg/L     cu m/d     cu m       ug/kg   mg/kg
                // Atmospheric and point-source loadings should be to epilimnion in single-segment mode;  9/9/98
                if (AQTSeg.LinkedMode || (AQTSeg.VSeg == VerticalSegments.Epilimnion))
                {
                    for (Loop = Alt_LoadingsType.PointSource; Loop <= Alt_LoadingsType.NonPointSource; Loop++)
                    {
                        if (Loop != Alt_LoadingsType.DirectPrecip)
                        {
                            // Irrelevant for Susp,Dissolved Detritus and Sediment
                            if (NState >= AllVariables.DissRefrDetr && NState <= AllVariables.SuspLabDetr)
                            {
                                // Split into two or four compartments
                                AddLoad = Loadings.Units.Loadings.ReturnAltLoad(TimeIndex, PInputRec.Load, Loop) * ((CPtr) as TDetritus).MultFrac(TimeIndex, true, Loop);
                                // g/d
                                // unitless
                                ToxLoad = Loadings.Units.Loadings.ReturnAltLoad(TimeIndex, PInputRec.ToxLoad[SVType], Loop);
                                // ug/kg
                            }
                            else
                            {
                                AddLoad = Loadings.Units.Loadings.ReturnAltLoad(TimeIndex, CPtr.LoadsRec, Loop);
                                ToxLoad = Loadings.Units.Loadings.ReturnAltLoad(TimeIndex, LoadsRec, Loop);
                            }
                            AddLoad = AddLoad / SegVolume * ToxLoad / 1e6;
                            // ug/L
                            // g/d
                            // cu m
                            // ug/kg
                            // mg/kg
                            LoadRes = LoadRes + AddLoad;
                            // mg/L d
                            // mg/L d
                        }
                    }
                }
                Loading = LoadRes;
                return;
                // Loading calculation is complete for Loadings of Tox in Susp&Diss Detritus
            }

            if ((new ArrayList(new object[] { Consts.FirstDetr, Consts.FirstBiota }).Contains(NState)))
            {
                // Toxicants within Organisms Loadings
                // Calc CarrierLdg, the inflow loading of the carrier
                CPtr = AQTSeg.GetStatePointer(Carrier, T_SVType.StV, T_SVLayer.WaterCol);
                CarrierLdg = Loadings.Units.Loadings.ReturnLoad(TimeIndex, CPtr.LoadsRec) * Inflow / SegVolume;
                // ug/kg
                base.CalculateLoad(TimeIndex);
                // TStateVariable
                // Put the organism's inflow tox load in ug/kg into the Loading variable
                // sediment input in dry weight units
                if (NState >= AllVariables.SedmRefrDetr && NState <= AllVariables.SuspLabDetr)
                {
                    Wet2Dry = 1.0;
                }
                else
                {
                    Wet2Dry = WetToDry();
                }
                if ((new ArrayList(new object[] { AllVariables.SedmRefrDetr, AllVariables.SedmLabDetr }).Contains(NState)) && AQTSeg.PSetup.TSedDetrIsDriving)
                {
                    // 6/7/2013  Directly assign toxicants to sediment on OC Norm basis
                    State = Loading * CPtr.State / 1e6;
                    // ug/L     ug/kg      mg/L      mg/kg
                    return;
                }
                // CarrierLdg is inflow loading only and has already been normalized for segment volume
                Loading = Loading * CarrierLdg / 1e6 * Wet2Dry;
                // ug/L d     ug/kg      mg/L      mg/kg  {loadings need conversion to dry weight
                LoadingsRecord _wvar2 = CPtr.LoadsRec;
                // 10/24/2012 handle loss of toxicant due to time-series fishing or withdrawal
                if ((NState >= Consts.FirstAnimal && NState <= Consts.LastAnimal))
                {
                    if (!(Alt_Loadings[Alt_LoadingsType.PointSource] == null))
                    {
                        for (Loop = Alt_LoadingsType.PointSource; Loop <= Alt_LoadingsType.DirectPrecip; Loop++)
                        {
                            // NPS Irrelevant for Fish
                            AddLoad = 0;
                            if (Alt_UseConstant[Loop])
                            {
                                // g/d or g/sq m. d
                                AddLoad = Alt_ConstLoad[Loop];
                            }
                            else if (Alt_Loadings[Loop] != null)
                            {
                                AddLoad = Alt_Loadings[Loop].GetLoad(TimeIndex, true);
                            }
                            AddLoad = AddLoad * Alt_MultLdg[Loop] / SegVolume;
                            // mg/L d
                            // g/d
                            // unitless
                            // cu m
                            if (Loop == Alt_LoadingsType.DirectPrecip)
                            {
                                AddLoad = AddLoad * Location.Locale.SurfArea;
                            }
                            // mg/L d
                            // mg/sq m. L d
                            // sq m.
                            // fish stocking is assumed clean, but need to track tox loss due to fishing/removal
                            if (AddLoad < 0)
                            {
                                Loading = Loading + AddLoad * AQTSeg.GetPPB(NState, SVType, Layer) * 1e-6;
                            }
                            // ug/L d
                            // ug/L d
                            // mg/L d
                            // ug/kg
                            // kg/mg
                        }
                    }
                }
                // loop
                return;
                // loading calculation is complete for toxicants within organisms
            }


            //if (AQTSeg.EstuarySegment && (AQTSeg.VSeg == VerticalSegments.Hypolimnion))
            //{
            //    return;
            //}  // estuary vsn. 10-17-2002

            // Inflow Loadings of Toxicant in Water
            base.CalculateLoad(TimeIndex);              // TStateVariable

            // if (AQTSeg.EstuarySegment) Inflow = Location.Morph.InflowH2O[VerticalSegments.Epilimnion];
            // upstream loadings only, estuary vsn. 10-17-02
            
            Loading = Loading * Inflow / SegVolume;
            // ug/L d     ug/L     cu m/d      cu m

            // Point Source Non-Point Source and Direct Precipitation Loadings of Toxicant in Water
            //if (AQTSeg.LinkedMode || (AQTSeg.VSeg == VerticalSegments.Epilimnion))
            {
                if ((!(LoadsRec.Alt_Loadings[0] == null)))
                {
                    for (Loop = 0; Loop <= 2; Loop++)
                    {
                        AddLoad = LoadsRec.ReturnAltLoad(TimeIndex, Loop);

                        AddLoad = (AddLoad  / SegVolume) * 1e3;
                        // ug/L d   // g/d   // cu m    // mg/g

                        // (mg/m3)=(ug/L)
                        if (Loop == 1 )  //  1 = Alt_LoadingsType.DirectPrecip)
                        {
                            AddLoad = AddLoad * Location.Locale.SurfArea;
                        }  // ug/L d  // ug/(sq m.)(L)(d)       // sq m.

                        Loading = Loading + AddLoad;
                        // ug/L d          // ug/L d
                    }
                }
            }
            // Loop

        }

        public override double WetToDry()
        {
            double result;
            TStateVariable PS;
            PS = AQTSeg.GetStatePointer(Carrier, T_SVType.StV, Layer);
            // dissolved in water
            // or in sediment
            if (new ArrayList(new object[] { Consts.FirstOrgTox, AllVariables.Sand, AllVariables.POC_G1 }).Contains(NState))
            {
                result = 1.0;
            }
            else
            {
                result = PS.WetToDry();
            }
            return result;
        }

        // (************************************)
        // (*      NonDissoc (Ionization)      *)
        // (************************************)
        public double NonDissoc()
        {
            double result;
            double pH_Val;   // pH Value
            double Charged;

            if (ChemRec.IsPFA)
            {
                result = 0;
                return result;
            }

            Charged = 0;
            // DissRefrDetr, PartRefrDetr,  suspLabDetr
            //if (NState >= Consts.FirstAnimal && NState <= Consts.LastAnimal)   fixme nondissoc code for animals
            //{
            //    Charged = -0.5;  // lower pH at gill surface--McKim & Erickson, 1991
            //}

            pH_Val = AQTSeg.GetState(AllVariables.pH, T_SVType.StV, T_SVLayer.WaterCol);
            if (ChemRec.pka == 0)
            {  result = 1;                                                              }
            else if (ChemRec.ChemIsBase)
            {  result = 1 / (1 + Math.Pow(10.0, ChemRec.pka - pH_Val));                 }
            else
            {  result = 1 / (1 + Math.Pow(10.0, (pH_Val + Charged - ChemRec.pka)));     }

            return result;
        }

        // (************************************)
        // (*            Discharge             *)
        // (************************************)
        public override double Washout()
        {
            double result;
            if (NState != AllVariables.H2OTox)
            {
                throw new Exception("Programming Error, TToxics.Washout must be passed Tox Dissolved in Water");
            }
            result = base.Washout();
            // TStateVariable

            return result;
        }


        public double CalculateKOM()
        {
            double result;
            // Used in Detrital Sorption, Desorption below
            double KOM;
            double KOW;
            double IonCorr;
            double NonDiss;
            if ((NState < AllVariables.ReDOMPore) || (NState > Consts.LastDetr) || (SVType < Consts.FirstOrgTxTyp) || (SVType <= Consts.LastOrgTxTyp))
            {
                throw new Exception("Programming Error, CalculateKOM must be passed a detrital toxicant");
            }
            KOW = Math.Pow(10, ChemRec.LogKow);

            if (ChemRec.ChemIsBase)
            {   IonCorr = 0.01;     }
            else
            {   IonCorr = 0.1;      }

            if (ChemRec.IsPFA)
            {
                result = ChemRec.PFASedKom;
                return result;
            }
            NonDiss = NonDissoc();
            // prevent multiple calculations
            KOM = -9999;

            if ((NState == AllVariables.SedmRefrDetr) && (!ChemRec.CalcKPSed))
            {
                KOM = ChemRec.KPSed * 0.526;
                // L/kg OM = L/kg OC * g OC / g OM
            }

            // translate user entered value to proper units
            if (((NState == AllVariables.DissRefrDetr)||(NState == AllVariables.ReDOMPore )) && (!ChemRec.CalcKOMRefrDOM))
            {
                KOM = ChemRec.KOMRefrDOM;
            }
            if ((KOM < 0))
            {
                switch (NState)
                {
                    case AllVariables.DissRefrDetr:
                    case AllVariables.ReDOMPore:
                        KOM = (NonDiss * 2.88 * Math.Pow(KOW, 0.67) + (1 - NonDiss) * IonCorr * 2.88 * Math.Pow(KOW, 0.67)) * 0.526;
                        break;
                    case AllVariables.DissLabDetr:
                    case AllVariables.LaDOMPore:
                        // generalized from Freidig et al. 1998,  Modified 3/13/2009
                        KOM = (NonDiss * 0.88 * KOW + (1 - NonDiss) * IonCorr * 0.88 * KOW) * 0.526;
                        break;
                    case AllVariables.SedmLabDetr:
                    case AllVariables.SuspLabDetr:
                    case AllVariables.POC_G1:
                        // generalized from Koelmans and Heugens 1998
                        KOM = (NonDiss * 23.44 * Math.Pow(KOW, 0.61) + (1 - NonDiss) * IonCorr * 23.44 * Math.Pow(KOW, 0.61)) * 0.526;
                        break;
                    default:
                        // based on fresh algal detritus parameters in Koelmans, Anzion, & Lijklema 1995
                        KOM = (NonDiss * 1.38 * Math.Pow(KOW, 0.82) + (1 - NonDiss) * IonCorr * 1.38 * Math.Pow(KOW, 0.82));
                        break;
                        // SedRefr/suspRefr generalized from Schwarzenbach et al. 1993, p. 275 and Smejtek and Wang, 1993, for ionized compound
                }
            }
            // case
            result = KOM;
            return result;
        }

        // ------------------------------------------------------------------------------------------------------------------
        public double Sorption()
        {
            double result;
            // Calculate Organic Chemical Sorption to Sediments/ Detritus
            // Returns units of ug/L, Liters of water column or pore water depending on location of sed-detrital toxicant passed
            double CarrierState, K1, UptakeLimit, ToxState, Kp, Diff1;
            bool ToxIsPoreW;

            //          T_SVLayer PoreLayer;
            //          TPorewater ThisPore;

            result = 0;

            //PoreLayer = Layer;
            //if (PoreLayer == T_SVLayer.WaterCol)
            //{   PoreLayer = T_SVLayer.SedLayer1; }

            // utility var for sediments
            if ((NState < AllVariables.ReDOMPore) || (NState > Consts.LastDetr))
                throw new Exception("Programming Error: Sorption was passed a non sediment/detritus");

            // K1
            switch (NState)
            {
                case AllVariables.Cohesives:  // inorganic sorption
                    K1 = ChemRec.CohesivesK1; break;
                case AllVariables.NonCohesives:
                    K1 = ChemRec.NonCohK1; break;
                case AllVariables.NonCohesives2:
                    K1 = ChemRec.NonCoh2K1; break;
                default:  // organics
                    K1 = ChemRec.K1Detritus;  // calibrated to 1.39  
                    break;
            }

            if (K1 == 0) return 0;

            // ToxState
            ToxState = 0;
            if ((Layer == T_SVLayer.WaterCol) || (NState >= AllVariables.POC_G1 && NState <= AllVariables.POC_G3))
                ToxState = AQTSeg.GetState(AllVariables.H2OTox, SVType, T_SVLayer.WaterCol);
            // ug/L wc

            // Sed Detritus is labeled as being in the "WaterCol" though it really resides in SedLayer1
            // This is to provide compatibility when the model runs without the Sediment Sub Model
            ToxIsPoreW = false;

            //if (AQTSeg.SedModelIncluded())
            //{
            //    ToxIsPoreW = (Layer > T_SVLayer.WaterCol) || (NState >= AllVariables.SedmRefrDetr && NState <= AllVariables.SedmLabDetr);
            //    if (ToxIsPoreW)
            //    { ToxState = AQTSeg.GetState(AllVariables.PoreWater, SVType, PoreLayer);  }
            //    // ug/L pw
            //}

            if (ToxState <= Consts.Tiny) return 0;

            // Michaelis
            switch (NState)
            {
                case AllVariables.Cohesives:
                    Kp = ChemRec.CohesivesKp; break;
                case AllVariables.NonCohesives:
                    Kp = ChemRec.NonCohKp; break;
                case AllVariables.NonCohesives2:
                    Kp = ChemRec.NonCoh2Kp; break;
                default:  // organics
                    Kp = CalculateKOM(); break;
            }  // Case

            if (Kp == 0) return 0;

            UptakeLimit = (Kp * ToxState - AQTSeg.GetPPB(NState, SVType, Layer)) / (Kp * ToxState);
            if (UptakeLimit < 0) UptakeLimit = 0;

            // NonDissoc is a separate function (TToxics.NonDissoc) found above

            CarrierState = AQTSeg.GetState(NState, T_SVType.StV, Layer);
            // mg/L or g/m2 (buried)

            //if (ToxIsPoreW && (NState > AllVariables.LaDOMPore))
            //{
            //    // Carrerstate must be converted to mg/L(porewater)
            //    ThisPore = AQTSeg.GetStatePointer(AllVariables.PoreWater, T_SVType.StV, PoreLayer);
            //    if (ThisPore.VolumeInM3() < Consts.Tiny) return 0;

            //    if ((NState >= AllVariables.Cohesives && NState <= AllVariables.NonCohesives2) || (Layer > T_SVLayer.SedLayer1))
            //    {   // mg/L pw       // g/m2          // m2                 // m3 pw
            //        CarrierState = CarrierState * SedLayerArea() / ThisPore.VolumeInM3();
            //    }
            //    else
            //    {   CarrierState = CarrierState * SegVol() / ThisPore.VolumeInM3();
            //        // mg/L pw     // mg/L wc     // m3 wc       // m3 pw
            //    }
            //}

            if (!(NState >= AllVariables.POC_G1 && NState <= AllVariables.POC_G3))
            {
                result = K1 * ToxState * UptakeLimit * CarrierState * 1e-6;
                // ug/L-d = L/kgdry-d * ug/L * unitless  * mg/L * kg/mg
            }
            else
            {   // Diagenesis Units
                result = K1 * ToxState * UptakeLimit * CarrierState * 1e-3 * Consts.Detr_OM_2_OC;
                // ug/m2-d = L/kgdry OM-d * ug tox/L *   unitless * g POC/m2 * kg/g * OM/POC
            }

            return result;
        }

        // ------------------------------------------------------------------------------------------------------------------
        public double Desorption()
        {
            // Calculate Organic Desorption for Sediments/ Detritus
            double Desorp;
            double K2;
            double KOM;
            if (!(NState >= AllVariables.ReDOMPore && NState <= Consts.LastDetr))
            {
                throw new Exception("Programming Error: Desorption was passed a non sediment/detritus");
            }
            Desorp = 0;
            if (State <= 0) {  return Desorp;  }

            //default:  // organics
                    KOM = CalculateKOM();
                    if (KOM == 0)
                    {  K2 = 0; }
                    else
                    {  K2 = ChemRec.K1Detritus / KOM;   }
                    // (1/(0.72 * KPSed))
                    // 9/25/2011 was hard-wired to 1.39
                    // Karickoff's obs.
            //break;
            //}

            Desorp =  K2 * State;
        // {ug/L-d}  {1/d} {ug/L}  {water col units}
            if (Desorp < 0) Desorp = 0;

            return Desorp;
        }


        // (************************************)
        // (*                                  *)
        // (*     DIFFERENTIAL EQUATIONS       *)
        // (*                                  *)
        // (************************************)
        public override void Derivative(ref double DB)
        {
            // Derivative for Toxicant Dissolved in Water
            //double ThisPPB =0;
            //double Dec =0;
            double Lo =0;
            double Hyd =0;
            double Pho =0;
            double Mic =0;
            double Vl =0;
            double ToxDis =0;
            double Inflow =0;
            double TDF =0;
            double FracAerobic =0;
            double Mic_in_Aer =0;
            double Mic_in_Anaer =0;
            double DiffUp =0;
            double DetrSorption =0;
            double DetrDesorption =0;
            double PlantSorp =0;
            double InorgSorpt =0;
            double InorgDesorpt =0;
            double Entr =0;
            double GillSorption =0;
            double Dep =0;
            double Decomp =0;
            double DiffDown =0;
            double DiffSed =0;
            double PoreWUp =0;
            double PoreWDown =0;
            //double PWToxLevel =0;
            //double FracInWater =0;
            //double FracPoreWUp =0;
            //double OOSDriftInKg =0;
            //double LoadInKg =0;
            //TPorewater ToTPoreWater;
            //TPoreWaterTox ToTPoreWaterTox;
            TToxics PT;
            //TDetritus PD;
            //TPOC_Sediment TPOC;
            //TPOCTox TPOCT;
            AllVariables NsLoop;
            //AllVariables StartLoop;
            //AllVariables DecompLoop;
            //TAlgae_ZooTox AlgalToxPtr;
            bool SedModelRunning;
            //double UnitFix;
            //double DissSorpInKG;
            // Derivative for Toxicant Dissolved in Water
            SedModelRunning = AQTSeg.GetStatePointer(AllVariables.PoreWater, T_SVType.StV, T_SVLayer.SedLayer1) != null;

            if (IsAGGR)  // is this state variable merely in place to aggregate the concentration of many components?
            {
                DB = 0.0;
//                Derivative_WriteRates();
                return;
            }
            if (AQTSeg.PSetup.ChemsDrivingVars)          // 4/5/2017 
            {
                base.CalculateLoad(AQTSeg.TPresent);     // TStateVariable
                DB = 0.0;
                State = Loading;
                return;
            }

            //if (Consts.Eutrophication || AQTSeg.PSetup.ChemsDrivingVars)
            //{   DB = 0.0;    }
            //else
            {
                CalculateLoad(AQTSeg.TPresent);
                Lo = Loading;
                //if (AQTSeg.EstuarySegment)   {  Entr = EstuaryEntrainment();     }

                //// save for tox loss output & categorization
                //if (Entr > 0)
                //{  LoadInKg = (Lo + Entr) * SegVol() * 1000.0 * 1e-9;     }
                //else
                //{  LoadInKg = Lo * _wvar3.SegVol() * 1000.0 * 1e-9;       }
                //// kg      // ug/L          // m3    // L/m3  // kg/ug

                //TotOOSLoad[DerivStep] = TotOOSLoad[_wvar3.DerivStep] + LoadInKg;
                //ToxLoadH2O[DerivStep] = ToxLoadH2O[_wvar3.DerivStep] + LoadInKg;

                Hyd = Hydrolysis();
                Pho = Photolysis();
                Mic = MicrobialMetabolism(ref FracAerobic);

                //Mic_in_Aer = Microbial_BioTrans_To_This_SV(true);
                //Mic_in_Anaer = Microbial_BioTrans_To_This_SV(false);

                Vl = Volatilization();
                ToxDis = Washout();

                // save for tox loss output & categorization
                //if (Entr < 0) { OOSDriftInKg = (-Entr + ToxDis) * _wvar5.SegVol() * 1000.0 * 1e-9; } // * OOSDISCHFRAC
                //else          { OOSDriftInKg = ToxDis * _wvar5.SegVol() * 1000.0 * 1e-9; }  // * OOSDISCHFRAC 
                //                }  // kg        // ug/L      // m3     // L/m3     // kg/ug
                //TotalToxLoss[_wvar5.DerivStep] = _wvar6.TotalToxLoss[_wvar5.DerivStep] + OOSDriftInKg;
                //TotalWashout[_wvar5.DerivStep] = _wvar6.TotalWashout[_wvar5.DerivStep] + OOSDriftInKg;
                //WashoutH2O[_wvar5.DerivStep] = _wvar6.WashoutH2O[_wvar5.DerivStep] + OOSDriftInKg;
                //DissWash[_wvar5.DerivStep] = _wvar6.DissWash[_wvar5.DerivStep] + OOSDriftInKg;

                //if (LinkedMode)
                //{
                //    Inflow = WashIn();
                //}
                //if (LinkedMode && (!CascadeRunning))
                //{
                //    DiffUp = SegmentDiffusion(true);
                //    DiffDown = SegmentDiffusion(false);
                //}
                //else if (!LinkedMode)
                //{
                //    TDF = TurbDiff();  // stratification
                //}

                //ToTPoreWaterTox = AQTSeg.GetStatePointer(AllVariables.PoreWater, ToxType, T_SVLayer.SedLayer1);   // Diffusion from/to pore waters
                //if (ToTPoreWaterTox != null)
                //{   DiffSed = -ToTPoreWaterTox.UpperDiffusion(true);     }
                
                InorgSorpt = 0;
                InorgDesorpt = 0;
                if (SedModelRunning)
                {
                    for (NsLoop = AllVariables.Cohesives; NsLoop <= AllVariables.NonCohesives2; NsLoop++)
                    {
                        PT = (TToxics) AQTSeg.GetStatePointer(NsLoop, SVType, T_SVLayer.WaterCol);
                        if ((PT != null))
                        {
                            // InorgSorpt = InorgSorpt + PT.Sorption();  
                            InorgDesorpt = InorgDesorpt + PT.Desorption();
                        }
                    }
                }

                //ToTPoreWater = AQTSeg.GetStatePointer(AllVariables.PoreWater, T_SVType.StV, T_SVLayer.SedLayer1);  // Pore water interface
                //if ((ToTPoreWater != null))
                //{
                //    if ((ToTPoreWater.VolumeInM3() > Consts.Tiny))
                //    {
                //        PWToxLevel = AQTSeg.GetState(AllVariables.PoreWater, ToxType, T_SVLayer.SedLayer1);
                //        // ug/L pw
                //        FracPoreWUp = ToTPoreWater.To_Above() / _wvar8.PWVol_Last_Step[T_SVLayer.SedLayer1];
                //        // frac/d            // m3/m2 d                    // m3/m2
                //        PoreWUp = PWToxLevel * FracPoreWUp * ToTPoreWater.VolumeInM3() / AQTSeg.SegVol();
                //        // ug/L(wc) d =  ug/L pw * fraction/d *  m3 pw / m3 wc
                //        // wc=watercolumn, pw=porewater

                //        PoreWDown = State * ToTPoreWater.From_Above() * SedLayerArea() / SegVol();
                //        // ug/L(wc) d  = ug/L wc *         m3/m2 d    *        m2      /     m3
                //    }
                //}

                if (!ChemRec.BCFUptake)
                {
                    // Animal and plant sorption / desorption
                    // If the sed submodel is being utilized then seddetr
                    //if (SedModelRunning)
                    //{
                    //    // decomposition, sorption and desorption is
                    //    StartLoop = AllVariables.DissRefrDetr;
                    //}
                    //else
                    //{
                    //    StartLoop = AllVariables.SedmRefrDetr;
                    //}
                    // associated with the tox in the active layer's pore water
                    // not the tox in the water column . . .
                    //Decomp = 0;
                    //for (DecompLoop = StartLoop; DecompLoop <= Consts.LastDetr; DecompLoop++)
                    //{
                    //    PD = (TDetritus) AQTSeg.GetStatePointer(DecompLoop, T_SVType.StV, T_SVLayer.WaterCol);
                    //    if ((PD != null))
                    //    {
                    //        Dec = PD.Decomposition(Location.Remin.DecayMax_Lab, Consts.KAnaerobic, ref FracAerobic);
                    //        ThisPPB = GetPPB(DecompLoop, SVType, T_SVLayer.WaterCol);
                    //        Decomp = Decomp + (Dec * ThisPPB * 1e-6);
                    //    }
                    //}

                    //if (AQTSeg.Diagenesis_Included())
                    //{
                    //    for (DecompLoop = AllVariables.POC_G1; DecompLoop <= AllVariables.POC_G3; DecompLoop++)
                    //    {
                    //        TPOC = AQTSeg.GetStatePointer(DecompLoop, T_SVType.StV, T_SVLayer.SedLayer2);
                    //        if ((TPOC != null))
                    //        {
                    //            Dec = TPOC.Mineralization();
                    //            // g/m3 sed. d
                    //            ThisPPB = GetPPB(DecompLoop, SVType, T_SVLayer.SedLayer2);
                    //            Decomp = Decomp + (Dec * ThisPPB * 1e-6) * AQTSeg.DiagenesisVol(2) / Location.Morph.SegVolum;
                    //            // ug/L day =  mg/L sed. d + ( ug/kg * kg/mg) *  m3 sed           /  m3 water
                    //        }
                    //    }
                    //}

                    //DetrSorption = 0;
                    //DetrDesorption = 0;
                    //for (NsLoop = StartLoop; NsLoop <= Consts.LastDetr; NsLoop++)
                    //{
                    //    PT = (TToxics) AQTSeg.GetStatePointer(NsLoop, SVType, T_SVLayer.WaterCol);
                    //    if ((PT != null))
                    //    {
                    //        // DetrSorption = DetrSorption + PT.Sorption();
                    //        DetrDesorption = DetrDesorption + PT.Desorption();
                    //    }
                    //}

                    //if (AQTSeg.Diagenesis_Included())
                    //{
                    //    for (NsLoop = AllVariables.POC_G1; NsLoop <= AllVariables.POC_G3; NsLoop++)
                    //    {
                    //        UnitFix = Location.Locale.SurfArea / (AQTSeg.SegVol() * 1000);
                    //        // m2/L                   // m2            // m3     // L/m3
                    //        TPOCT = AQTSeg.GetStatePointer(NsLoop, ToxType, T_SVLayer.SedLayer2);
                    //        if ((TPOCT != null))
                    //        {
                    //            DetrSorption = DetrSorption + TPOCT.Sorption() * UnitFix;
                    //            DetrDesorption = DetrDesorption + TPOCT.Desorption() * UnitFix;
                    //            // ug/L d          // ug/m2 d           // m2/L                 }
                    //    }
                    //}

                    //PlantSorp = 0;
                    //for (NsLoop = Consts.FirstPlant; NsLoop <= Consts.LastPlant; NsLoop++)
                    //{
                    //    AlgalToxPtr = (AQTSeg.GetStatePointer(NsLoop, ToxType, T_SVLayer.WaterCol));
                    //    if (!(AlgalToxPtr == null))
                    //    {
                    //        PlantSorp = PlantSorp + AlgalToxPtr.PlantUptake();
                    //    }
                    //}

                    //GillSorption = 0;
                    //for (NsLoop = Consts.FirstAnimal; NsLoop <= Consts.LastAnimal; NsLoop++)
                    //{
                    //    if (!(AQTSeg.GetStatePointer(NsLoop, T_SVType.StV, T_SVLayer.WaterCol) == null))
                    //    {
                    //        GillSorption = GillSorption + ((AQTSeg.GetStatePointer(NsLoop, T_SVType.StV, T_SVLayer.WaterCol)) as TAnimal).GillUptake(ToxType, T_SVLayer.WaterCol);
                    //    }
                    //}

                    //// save for tox loss output & categorization
                    //DissSorpInKG = (DetrSorption + PlantSorp + GillSorption) * Volume_Last_Step * 1000.0 * 1e-9;
                    //// kg = ( ug/L + ug/L + ug/L ) *  m3 *  L/m3 * kg/ug
                    //DissSorp[DerivStep] = DissSorp[DerivStep] + DissSorpInKG;

                    //ToTPoreWater = AQTSeg.GetStatePointer(AllVariables.PoreWater, T_SVType.StV, T_SVLayer.SedLayer1);
                    //Dep = 0;
                    //for (NsLoop = Consts.FirstBiota; NsLoop <= Consts.LastBiota; NsLoop++)
                    //{
                    //    PT = (AQTSeg.GetStatePointer(NsLoop, Consts.AssocToxTyp(NState), T_SVLayer.WaterCol));
                    //    if (PT != null)
                    //    {
                    //        FracInWater = 1;
                    //        if (SedModelRunning && (NsLoop >= Consts.FirstAnimal && NsLoop <= Consts.LastAnimal) && (ToTPoreWater.VolumeInM3() > Consts.Tiny))
                    //        {
                    //            FracInWater = ((AQTSeg.GetStatePointer(NsLoop, T_SVType.StV, T_SVLayer.WaterCol)) as TAnimal).PAnimalData.FracInWaterCol;
                    //        }
                    //        Dep = Dep + PT.Depuration() * FracInWater;
                    //    }
                    //}
                }

                if (GillUptake_Link != null) GillSorption = GillUptake_Link.ReturnLoad(AQTSeg.TPresent);
                if (Depuration_Link != null) Dep = Depuration_Link.ReturnLoad(AQTSeg.TPresent);
                if (Sorption_Link != null) DetrSorption = Sorption_Link.ReturnLoad(AQTSeg.TPresent);
                if (Decomposition_Link != null) Decomp = Decomposition_Link.ReturnLoad(AQTSeg.TPresent);
                if (Desorption_Link != null) DetrDesorption = Desorption_Link.ReturnLoad(AQTSeg.TPresent);
                if (PlantUptake_Link != null) PlantSorp = PlantUptake_Link.ReturnLoad(AQTSeg.TPresent);

                DB = Lo - Hyd - Pho - Mic - Vl - ToxDis + Inflow + TDF + Mic_in_Aer + Mic_in_Anaer + DiffUp + DiffDown + DiffSed + Entr;
                DB = DB + Decomp - DetrSorption + DetrDesorption - InorgSorpt + InorgDesorpt - GillSorption + Dep - PlantSorp + PoreWUp - PoreWDown;
            }  // If Not Estimate By BCF

           //  Derivative_WriteRates();
        }

    } // end TToxics



}
