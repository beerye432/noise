import React, { Component } from 'react';
import './stylesheets/noise.css';
import { Route } from 'react-router';

export default class Home extends Component {
    static displayName = Home.name;

    render() {
        return (
            <div className="noise-grid"></div>
        );
    }
}
