﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms.DataVisualization.Charting;

namespace LogicLink.Corona {

    /// <summary>
    /// Charting series view of a SEIR object. X-axis is a time range.
    /// </summary>
    public class SEIRDateSeriesView : IDateSeriesView {
        private readonly ISEIR _seir;               // SEIR-object

        private readonly Series _serSusceptible;    // Series of the number of individuals in the S(usceptible) compartment
        private readonly Series _serExposed;        // Series of the number of individuals in the E(xposed) compartment
        private readonly Series _serInfectious;     // Series of the number of individuals in the I(nfectious) compartment
        private readonly Series _serRemoved;        // Series of the number of individuals in the R(emoved) compartment
        private readonly Series _serCases;          // Series of the total number of confirmed infected individuals ( Number of E(xposed) + I(nfectious) + R(emoved) )
        private readonly Series _serDaily;          // Series of the number of confirmed infected individuals for the day ( Cases - Cases of the previous day )
        private readonly Series _ser7Days;          // Series of the 7 day average of daily cases per 100.000 individuals
        private readonly Series _serReproduction;   // Series of the basic reproduction number (R₀)

        private readonly bool _bDoubledMarker;      // If true, double value diamond markers are added to cases-, daily- and 7days-series

        /// <summary>
        /// Creates and initializes a new view object
        /// </summary>
        /// <param name="seir">SEIR-object</param>
        /// <param name="bSusceptible">If true, susceptible-series is shown.</param>
        /// <param name="bExposed">>If true, exposed-series is shown.</param>
        /// <param name="bInfectious">If true, infectious-series is shown.</param>
        /// <param name="bRemoved">If true, removed-series is shown.</param>
        /// <param name="bCases">If true, total cases-series is shown.</param>
        /// <param name="bDaily">If true, daily cases-series is shown.</param>
        /// <param name="b7Days">If true, 7 day average of daily cases per 100.000-series is shown.</param>
        /// <param name="bReproduction">If true, R₀-series is shown for the second axis</param>
        /// <param name="bDoubledMarker">If true, double value diamond markers are added to cases-, daily- and 7days-series</param>
        public SEIRDateSeriesView(ISEIR seir, bool bSusceptible = true, bool bExposed = true, bool bInfectious = true, bool bRemoved = true, bool bCases = true, bool bDaily = true, bool b7Days = true, bool bReproduction = true, bool bDoubledMarker = true) {
            _seir = seir;

            if(bSusceptible)
                _serSusceptible = new Series("Susceptible") { ChartType = SeriesChartType.Spline,
                                                              XValueType = ChartValueType.Date,
                                                              Color = Color.LightSkyBlue,
                                                              BorderWidth = 5 };

            if(bExposed)
                _serExposed = new Series("Exposed") { ChartType = SeriesChartType.Spline,
                                                      XValueType = ChartValueType.Date,
                                                      Color = Color.Orange,
                                                      BorderWidth = 5 };

            if(bInfectious)
                _serInfectious = new Series("Infectious") { ChartType = SeriesChartType.Spline,
                                                            XValueType = ChartValueType.Date,
                                                            Color = Color.Red,
                                                            BorderWidth = 5 };

            if(bRemoved)
                _serRemoved = new Series("Removed") { ChartType = SeriesChartType.Spline,
                                                      XValueType = ChartValueType.Date,
                                                      Color = Color.LimeGreen,
                                                      BorderWidth = 5 };

            if(bCases)
                _serCases = new Series("Cases") { ChartType = SeriesChartType.Spline,
                                                  XValueType = ChartValueType.Date,
                                                  Color = Color.Yellow,
                                                  BorderWidth = 5 };

            if(bDaily)
                _serDaily = new Series("Daily Cases") { ChartType = SeriesChartType.Column,
                                                        XValueType = ChartValueType.Date,
                                                        Color = Color.Yellow,
                                                        BorderWidth = 5 };

            if(b7Days)
                _ser7Days = new Series("7 Days Incidence") { ChartType = SeriesChartType.Column,
                                                             XValueType = ChartValueType.Date,
                                                             Color = Color.Goldenrod,
                                                             BorderWidth = 5 };

            if(bReproduction)
                _serReproduction = new Series("Reproduction R₀") { ChartType = SeriesChartType.Spline,
                                                                   YAxisType = AxisType.Secondary,
                                                                   XValueType = ChartValueType.Date,
                                                                   Color = Color.FromArgb(128, 128, 128),
                                                                   BorderWidth = 5 };

            _bDoubledMarker = bDoubledMarker;
        }

        /// <summary>
        ///  Calculates the series with the SEIR-object for a time range
        /// </summary>
        /// <param name="dtStart">Start date of the time range</param>
        /// <param name="dtEnd">End date of teh time range</param>
        /// <param name="p">Optional progress object</param>
        /// <returns>Awaitable task.</returns>
        public async Task CalcAsync(DateTime dtStart, DateTime dtEnd, IProgress<int> p = null) {
            int iPCount = 0;
            int iTotalDays = (dtEnd - dtStart).Days;
            int iPopulation = _seir.Susceptible + _seir.Exposed + _seir.Infectious + _seir.Removed;
            Queue<int> q7Days = new Queue<int>(7);
            int iCasesToday = 0;
            int iDailyToday = 0;
            int i7DaysToday = 0;
            for(DateTime dt = dtStart.AddDays(1d); dt <= dtEnd; dt = dt.AddDays(1d)) {
                int iCases = _seir.Exposed + _seir.Infectious + _seir.Removed;
                int iDays = (dt - dtStart).Days;

                _seir.Calc(iDays);

                if(_serSusceptible != null)
                    _serSusceptible.Points.AddXY(dt, _seir.Susceptible);
                if(_serExposed != null)
                    _serExposed.Points.AddXY(dt, _seir.Exposed);
                if(_serInfectious != null)
                    _serInfectious.Points.AddXY(dt, _seir.Infectious);
                if(_serRemoved != null)
                    _serRemoved.Points.AddXY(dt, _seir.Removed);
                if(_serCases != null) {
                    int i = _serCases.Points.AddXY(dt, _seir.Exposed + _seir.Infectious + _seir.Removed);
                    if(_bDoubledMarker && iCasesToday != 0 && iCasesToday * 2 < _seir.Exposed + _seir.Infectious + _seir.Removed) {
                        iCasesToday = 0;
                        _serCases.Points[i].MarkerStyle = MarkerStyle.Diamond;
                        _serCases.Points[i].MarkerSize = _serCases.BorderWidth * 2;
                        _serCases.Points[i].MarkerBorderColor = Color.DarkGray;
                    }

                }
                if(_serDaily != null) {
                    int i = _serDaily.Points.AddXY(dt, (Math.Max(_seir.Exposed + _seir.Infectious + _seir.Removed - iCases, 0)));  // Remarks: Rounding errors might lead to -1 as value. Thus, Math.Max([Cases], 0) is used
                    if(_bDoubledMarker && iDailyToday != 0 && iDailyToday * 2 < Math.Max(_seir.Exposed + _seir.Infectious + _seir.Removed - iCases, 0)) {
                        iDailyToday = 0;
                        _serDaily.Points[i].MarkerStyle = MarkerStyle.Diamond;
                        _serDaily.Points[i].MarkerSize = _serDaily.BorderWidth * 2;
                        _serDaily.Points[i].MarkerBorderColor = Color.DarkGray;
                    }
                }

                if(_ser7Days != null) {
                    q7Days.Enqueue(Math.Max(_seir.Exposed + _seir.Infectious + _seir.Removed - iCases, 0));
                    while(q7Days.Count > 7)
                        q7Days.Dequeue();
                    int i = _ser7Days.Points.AddXY(dt, Math.Round(q7Days.Sum() / (iPopulation / 100000d), 2));
                    if(_bDoubledMarker && i7DaysToday != 0 && i7DaysToday * 2 < q7Days.Sum()) {
                        i7DaysToday = 0;
                        _ser7Days.Points[i].MarkerStyle = MarkerStyle.Diamond;
                        _ser7Days.Points[i].MarkerSize = _ser7Days.BorderWidth * 2;
                        _ser7Days.Points[i].MarkerBorderColor = Color.DarkGray;
                    }
                }

                if(_serReproduction != null)
                    _serReproduction.Points.AddXY(dt, _seir.Reproduction);
                
                if(_bDoubledMarker && dt == DateTime.Today) {
                    iCasesToday = _seir.Exposed + _seir.Infectious + _seir.Removed;
                    iDailyToday = Math.Max(_seir.Exposed + _seir.Infectious + _seir.Removed - iCases, 0);
                    i7DaysToday = q7Days.Sum();
                }

                if(iPCount != 25 * iDays / iTotalDays) {
                    iPCount = 25 * iDays / iTotalDays;
                    p?.Report(4 * iPCount);
                }
            }
            await Task.CompletedTask;
        }

        #region IEnumerable Interface

        /// <summary>
        /// Enumerates charting series
        /// </summary>
        /// <returns>Enumerable of charting series</returns>
        public IEnumerator<Series> GetEnumerator() {
            if(_serSusceptible != null)
                yield return _serSusceptible;
            if(_serExposed != null)
                yield return _serExposed;
            if(_serInfectious != null)
                yield return _serInfectious;
            if(_serRemoved != null)
                yield return _serRemoved;
            if(_serCases != null)
                yield return _serCases;
            if(_serDaily != null)
                yield return _serDaily;
            if(_ser7Days != null)
                yield return _ser7Days;
            if(_serReproduction != null)
                yield return _serReproduction;
        }

        /// <summary>
        /// Untyped version of the typed enumerator
        /// </summary>
        /// <returns>Enumerable of object</returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion
    }
}
