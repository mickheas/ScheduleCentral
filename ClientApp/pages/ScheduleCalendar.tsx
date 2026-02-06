import React from 'react';

const ScheduleCalendar: React.FC = () => {
    return (
        <div>
            <h1 className="text-2xl font-bold text-gray-900">Schedule Calendar</h1>
            <p className="mt-4">
                This is where the main schedule (e.g., using FullCalendar) will be displayed.
            </p>
            {/* TODO: Add FullCalendar component here */}
            <div className="mt-6 h-96 w-full rounded-lg border border-dashed border-gray-400 bg-gray-50 flex items-center justify-center">
                <span className="text-gray-500">Calendar View Placeholder</span>
            </div>
        </div>
    );
};

export default ScheduleCalendar;