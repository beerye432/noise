import React, { Component } from 'react';
import './stylesheets/noise.css';
import { Route } from 'react-router';

export default class Home extends Component {
    static displayName = Home.name;

    render() {
        let grid = Array.from(Array(512).keys()).map(function (item, index) {
            let color = "rgb(" + index + "," + index / 3 + "," + index / 2 + ")";
            let s = {
                color: color,
                backgroundColor: color
            }
            return <div key={index} style={s} className={"grid-cell"}></div>
        });

        return (
            <div className="noise-grid">{grid}</div>
        );
    }
}
