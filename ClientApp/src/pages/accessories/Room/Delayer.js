import React, { useEffect, useRef } from "react";

export default function (props) {
    const div = useRef();
    useEffect(() => {
        div.current.focus();
    })
    return <div ref={div} id="loading" style={{opacity: 1, backgroundColor: "rgba(255, 255, 255, .3)"}} className="cover" tabIndex="-1">
        <div id="dcont">
            <div id="logo">
                Rooms
            </div>
            <div id="sec">
                {props.sec}
            </div>
        </div>
    </div>
}