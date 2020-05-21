import React, { useEffect, useRef } from "react";

export default function (props) {
    const div = useRef();
    const logo = useRef();
    useEffect(() => {
        div.current.focus();
        setTimeout(() => {
            logo.current.style.opacity = 0;
        }, 100);
    })
    return <div ref={div} id="loading" style={{ opacity: 1, backgroundColor: "rgba(255, 255, 255, .3)" }} className="cover" tabIndex="-1">
        <div id="dcont">
            <div id="logo" ref={logo}>
                Rooms
            </div>
        </div>
    </div>
}