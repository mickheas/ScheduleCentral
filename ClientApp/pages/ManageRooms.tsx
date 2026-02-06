import React, { useState, useEffect } from 'react';

// TODO: Define a 'Room' type based on your Model
interface Room {
    id: number;
    roomNumber: string;
    capacity: number;
    roomType: string;
}

const ManageRooms: React.FC = () => {
    const [rooms, setRooms] = useState<Room[]>([]);
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

    return (
        <div>
            <div className="sm:flex sm:items-center">
                <div className="sm:flex-auto">
                    <h1 className="text-2xl font-bold text-gray-900">Manage Rooms</h1>
                    <p className="mt-2 text-sm text-gray-700">A list of all the rooms available for scheduling.</p>
                </div>
                <div className="mt-4 sm:mt-0 sm:ml-16 sm:flex-none">
                    <button type="button" className="inline-flex items-center justify-center rounded-md border border-transparent bg-indigo-600 px-4 py-2 text-sm font-medium text-white shadow-sm hover:bg-indigo-700 ...">
                        Add Room
                    </button>
                </div>
            </div>

            {/* Room List Table */}
            <div className="mt-8 flex flex-col">
                <div className="-my-2 -mx-4 overflow-x-auto sm:-mx-6 lg:-mx-8">
                    <div className="inline-block min-w-full py-2 align-middle md:px-6 lg:px-8">
                        <div className="overflow-hidden shadow ring-1 ring-black ring-opacity-5 md:rounded-lg">
                            <table className="min-w-full divide-y divide-gray-300">
                                <thead className="bg-gray-50">
                                    <tr>
                                        <th scope="col" className="py-3.5 pl-4 pr-3 text-left text-sm font-semibold text-gray-900 sm:pl-6">Room Number</th>
                                        <th scope="col" className="px-3 py-3.5 text-left text-sm font-semibold text-gray-900">Capacity</th>
                                        <th scope="col" className="px-3 py-3.5 text-left text-sm font-semibold text-gray-900">Type</th>
                                        <th scope="col" className="relative py-3.5 pl-3 pr-4 sm:pr-6">
                                            <span className="sr-only">Edit</span>
                                        </th>
                                    </tr>
                                </thead>
                                <tbody className="divide-y divide-gray-200 bg-white">
                                    {rooms.map((room) => (
                                        <tr key={room.id}>
                                            <td className="whitespace-nowrap py-4 pl-4 pr-3 text-sm font-medium text-gray-900 sm:pl-6">{room.roomNumber}</td>
                                            <td className="whitespace-nowrap px-3 py-4 text-sm text-gray-500">{room.capacity}</td>
                                            <td className="whitespace-nowrap px-3 py-4 text-sm text-gray-500">{room.roomType}</td>
                                            <td className="relative whitespace-nowrap py-4 pl-3 pr-4 text-right text-sm font-medium sm:pr-6">
                                                <a href="#" className="text-indigo-600 hover:text-indigo-900">Edit</a>
                                            </td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default ManageRooms;