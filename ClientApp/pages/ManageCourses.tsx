import React from 'react';

// This is just a placeholder, you'd build this out like ManageRooms
const ManageCourses: React.FC = () => {
    return (
        <div>
            <h1 className="text-2xl font-bold text-gray-900">Manage Courses</h1>
            <p className="mt-4">
                This is where you will add/edit/delete courses (e.g., 'CS101', 'ARCH200').
            </p>
            <div className="mt-6 h-96 w-full rounded-lg border border-dashed border-gray-400 bg-gray-50 flex items-center justify-center">
                <span className="text-gray-500">Course Management Placeholder</span>
            </div>
        </div>
    );
};

export default ManageCourses;