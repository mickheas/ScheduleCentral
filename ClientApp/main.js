import { jsx as _jsx } from "react/jsx-runtime";
// import React from 'react';
// import ReactDOM from 'react-dom/client';
// import { BrowserRouter } from 'react-router-dom';
// import App from './App';
// // 1. Find the root element from our .cshtml file
// const rootElement = document.getElementById('react-dashboard-app');
// // 2. Find the session data script tag
// const sessionDataElement = document.getElementById('session-data');
// let sessionData = {
//   isAuthenticated: false,
//   email: '',
//   firstName: 'Guest',
//   roles: [],
// };
// if (sessionDataElement) {
//   try {
//     sessionData = JSON.parse(sessionDataElement.textContent || '{}');
//   } catch (error) {
//     console.error('Failed to parse session data:', error);
//   }
// }
// // 3. Render the React App
// if (rootElement) {
//   const root = ReactDOM.createRoot(rootElement);
//   root.render(
//     <React.StrictMode>
//       <BrowserRouter>
//         {/* Pass the session data into our main App component */}
//         <App session={sessionData} />
//       </BrowserRouter>
//     </React.StrictMode>
//   );
// }
import * as React from 'react';
import * as ReactDOM from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import App from './App';
const rootElement = document.getElementById('react-dashboard-app');
// Fallback for safety
const appContainer = rootElement || document.getElementById('react-app');
if (appContainer) {
    const defaultSession = { isAuthenticated: false, userName: 'Guest', email: '', roles: [] };
    const sessionData = window.APP_SESSION || defaultSession;
    console.log("React mounting with session:", sessionData);
    const root = ReactDOM.createRoot(appContainer);
    root.render(_jsx(React.StrictMode, { children: _jsx(BrowserRouter, { children: _jsx(App, { session: sessionData }) }) }));
}
else {
    console.error("Could not find root element 'react-dashboard-app'. React cannot mount.");
}
