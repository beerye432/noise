import React, { Component } from 'react';
import './stylesheets/noise.css';
import { Route } from 'react-router';

const axios = require('axios');

export default class Home extends Component {

    static displayName = Home.name;

    state = {
        sentiment: {},
        sentimentLoaded: false
    }

    componentDidMount() {
        axios.get("/sentiment/GetLatestSentiment?topic=1")
            .then(
            (result) => {
                    this.setState({ sentiment: result.data, sentimentLoaded: true });
                },
                (error) => {
                    console.log(error);
                }
            )
    }

    render() {
        if (this.state.sentimentLoaded) {

            const getColorModFromValence = function (valence, index) {
                const opacity = 1 - (index * 0.002);
                let color = `rgba(255,255, 0, ${opacity})`;
                if (valence > 0)
                    color = `rgba(${Math.max(0, 255 - (255 * valence))}, 255, 0, ${opacity})`;
                else if (valence < 0)
                    color = `rgba(255,${Math.min(255, 255 + (255 * valence))}, 0, ${opacity})`;

                return color;
            }

            const valence = this.state.sentiment.valence;

            const grid = Array.from(Array(512).keys()).map(function (item, index) {
                const s = {
                    backgroundColor: getColorModFromValence(valence, index)
                };
                const textStyle = {
                    color: "blue",
                    fontWeight: 600,
                    fontSize: "18px"
                };
                return <div key={index} style={s} className={"grid-cell"}><p style={textStyle}>{index === 0 ? valence : ""}</p></div>
            });

            return (
                <div className="noise-grid">
                    <div className="noise-grid-element">Topic: {this.state.sentiment.topic} <br /> Domain: {this.state.sentiment.domain}</div>
                    <div className="noise-grid-element">Date: {this.state.sentiment.date}</div>
                    <div className="noise-grid-element">Sentiment: {this.state.sentiment.valence}</div>
                </div>
            );
        }
        else {
            return null;
        }
    }
}
