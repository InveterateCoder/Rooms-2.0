import React from 'react';
import ReactDOM from 'react-dom';
import { BrowserRouter, withRouter } from 'react-router-dom';
import App from './App';
import registerServiceWorker from './registerServiceWorker';
//import { unregister } from './registerServiceWorker';

const baseUrl = document.getElementsByTagName('base')[0].getAttribute('href');
const rootElement = document.getElementById('root');
const Main = withRouter(App);

if (!navigator.webdriver) {
  ReactDOM.render(
    <BrowserRouter basename={baseUrl}>
      <Main />
    </BrowserRouter>,
    rootElement);

  registerServiceWorker();
  //unregister();
}

