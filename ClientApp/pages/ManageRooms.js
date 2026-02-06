import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { useState, useEffect } from 'react';
const ManageRooms = () => {
    const [rooms, setRooms] = useState([]);
    const [loading, setLoading] = useState(false);
    useEffect(() => {
        // TODO: Fetch data from your .NET API
        // setLoading(true);
        // fetch('/api/rooms')
        //   .then(res => res.json())
        //   .then(data => {
        //     setRooms(data);
        //     setLoading(false);
        //   });
        // Placeholder data
        setRooms([
            { id: 1, roomNumber: 'B302', capacity: 50, roomType: 'Lab' },
            { id: 2, roomNumber: 'F08', capacity: 100, roomType: 'Lecture Hall' },
            { id: 3, roomNumber: 'E102', capacity: 50, roomType: 'Studio' },
            { id: 4, roomNumber: 'F201', capacity: 50, roomType: 'Projector Room' },
            { id: 5, roomNumber: 'B204', capacity: 30, roomType: 'Normal Room' },
        ]);
    }, []);
    return (_jsxs("div", { children: [_jsxs("div", { className: "sm:flex sm:items-center", children: [_jsxs("div", { className: "sm:flex-auto", children: [_jsx("h1", { className: "text-2xl font-bold text-gray-900", children: "Manage Rooms" }), _jsx("p", { className: "mt-2 text-sm text-gray-700", children: "A list of all the rooms available for scheduling." })] }), _jsx("div", { className: "mt-4 sm:mt-0 sm:ml-16 sm:flex-none", children: _jsx("button", { type: "button", className: "inline-flex items-center justify-center rounded-md border border-transparent bg-indigo-600 px-4 py-2 text-sm font-medium text-white shadow-sm hover:bg-indigo-700 ...", children: "Add Room" }) })] }), _jsx("div", { className: "mt-8 flex flex-col", children: _jsx("div", { className: "-my-2 -mx-4 overflow-x-auto sm:-mx-6 lg:-mx-8", children: _jsx("div", { className: "inline-block min-w-full py-2 align-middle md:px-6 lg:px-8", children: _jsx("div", { className: "overflow-hidden shadow ring-1 ring-black ring-opacity-5 md:rounded-lg", children: _jsxs("table", { className: "min-w-full divide-y divide-gray-300", children: [_jsx("thead", { className: "bg-gray-50", children: _jsxs("tr", { children: [_jsx("th", { scope: "col", className: "py-3.5 pl-4 pr-3 text-left text-sm font-semibold text-gray-900 sm:pl-6", children: "Room Number" }), _jsx("th", { scope: "col", className: "px-3 py-3.5 text-left text-sm font-semibold text-gray-900", children: "Capacity" }), _jsx("th", { scope: "col", className: "px-3 py-3.5 text-left text-sm font-semibold text-gray-900", children: "Type" }), _jsx("th", { scope: "col", className: "relative py-3.5 pl-3 pr-4 sm:pr-6", children: _jsx("span", { className: "sr-only", children: "Edit" }) })] }) }), _jsx("tbody", { className: "divide-y divide-gray-200 bg-white", children: rooms.map((room) => (_jsxs("tr", { children: [_jsx("td", { className: "whitespace-nowrap py-4 pl-4 pr-3 text-sm font-medium text-gray-900 sm:pl-6", children: room.roomNumber }), _jsx("td", { className: "whitespace-nowrap px-3 py-4 text-sm text-gray-500", children: room.capacity }), _jsx("td", { className: "whitespace-nowrap px-3 py-4 text-sm text-gray-500", children: room.roomType }), _jsx("td", { className: "relative whitespace-nowrap py-4 pl-3 pr-4 text-right text-sm font-medium sm:pr-6", children: _jsx("a", { href: "#", className: "text-indigo-600 hover:text-indigo-900", children: "Edit" }) })] }, room.id))) })] }) }) }) }) })] }));
};
export default ManageRooms;
